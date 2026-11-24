using NugetScope.Core.Cli.ListPackage;
using NugetScope.Core.Cli.Restore;
using NugetScope.Core.Packages;
using NugetScope.Core.Scanning;
using NugetScope.Core.Solutions;

namespace NugetScope.Core.Cli;

/// <summary>
/// Package data via the dotnet CLI: one <c>dotnet restore</c>, then the base <c>dotnet list package</c> and one call per
/// status check, all with <c>--no-restore</c> and run in parallel (they only read restore output). A failing status
/// check is reported as a problem and left out of <see cref="PackageScanResult.CompletedChecks"/>; only a failing base
/// list fails the scan.
/// </summary>
public sealed class DotnetCliPackageSource(IProcessRunner processRunner) : IPackageSource
{
    public async Task<PackageScanResult> ScanAsync(
        SolutionInfo solution,
        PackageScanOptions options,
        IProgress<ScanStage>? progress = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RestoreDiagnostic> restoreDiagnostics = [];
        if (options.Restore)
        {
            progress?.Report(ScanStage.Restoring);
            restoreDiagnostics = await RestoreAsync(solution, cancellationToken).ConfigureAwait(false);
        }

        progress?.Report(ScanStage.ListingPackages);

        Task<ListPackageReport> baseList = ListAsync(solution, ListPackageQuery.Packages, options, cancellationToken);
        var statusLists = ListPackageArguments.StatusQueries(options.Checks)
            .Select(status => TryListStatusAsync(solution, status.Query, status.Check, options, cancellationToken))
            .ToList();

        await Task.WhenAll(statusLists.Append<Task>(baseList)).ConfigureAwait(false);

        var completedChecks = PackageChecks.None;
        var statusReports = new List<ListPackageReport>();
        var statusProblems = new List<PackageProblem>();

        foreach (StatusOutcome outcome in statusLists.Select(t => t.Result))
        {
            if (outcome.Report is not null)
            {
                statusReports.Add(outcome.Report);
            }

            if (outcome.Completed)
            {
                completedChecks |= outcome.Check;
            }
            else if (outcome.Failure is not null)
            {
                statusProblems.Add(outcome.Failure);
            }
        }

        ListPackageReport merged = ListPackageReportMerger.Merge(baseList.Result, statusReports);

        return ScanResultBuilder.Build(solution, merged, restoreDiagnostics, completedChecks, statusProblems);
    }

    private async Task<IReadOnlyList<RestoreDiagnostic>> RestoreAsync(SolutionInfo solution, CancellationToken cancellationToken)
    {
        ProcessResult result = await RunDotnetAsync(
                ["restore", solution.FilePath, "--verbosity", "quiet", "-nologo"],
                solution,
                cancellationToken)
            .ConfigureAwait(false);

        // A failed restore is not fatal: its errors are attached to the affected projects.
        return RestoreOutputParser.Parse(result.StandardOutput + "\n" + result.StandardError);
    }

    private async Task<ListPackageReport> ListAsync(
        SolutionInfo solution,
        ListPackageQuery query,
        PackageScanOptions options,
        CancellationToken cancellationToken)
    {
        var arguments = ListPackageArguments.Build(solution.FilePath, query, options.IncludeTransitive);
        ProcessResult result = await RunDotnetAsync(arguments, solution, cancellationToken).ConfigureAwait(false);

        try
        {
            // Exit code 1 is normal when any project has a problem; the JSON is still complete.
            return ListPackageParser.Parse(result.StandardOutput);
        }
        catch (FormatException ex)
        {
            throw new PackageSourceException(
                $"Could not read 'dotnet list package' output for the {Describe(query)} query (exit code {result.ExitCode}). {result.StandardError}".TrimEnd(),
                ex);
        }
    }

    private async Task<StatusOutcome> TryListStatusAsync(
        SolutionInfo solution,
        ListPackageQuery query,
        PackageChecks check,
        PackageScanOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            ListPackageReport report = await ListAsync(solution, query, options, cancellationToken).ConfigureAwait(false);

            // A solution-level error (e.g. an unreachable feed) means some packages went unchecked.
            bool completed = !report.Problems.Any(p => p.Level == ProblemLevel.Error);
            return new StatusOutcome(check, report, completed, Failure: null);
        }
        catch (PackageSourceException ex)
        {
            var failure = new PackageProblem(ProblemLevel.Warning, $"The {Describe(query)} check failed: {ex.Message}");
            return new StatusOutcome(check, Report: null, Completed: false, failure);
        }
    }

    private Task<ProcessResult> RunDotnetAsync(
        IReadOnlyList<string> arguments,
        SolutionInfo solution,
        CancellationToken cancellationToken) =>
        DotnetCommand.RunAsync(processRunner, arguments, solution.DirectoryPath, cancellationToken);

    private static string Describe(ListPackageQuery query) => query switch
    {
        ListPackageQuery.Outdated => "outdated",
        ListPackageQuery.Deprecated => "deprecated",
        ListPackageQuery.Vulnerable => "vulnerable",
        _ => "package list",
    };

    private sealed record StatusOutcome(PackageChecks Check, ListPackageReport? Report, bool Completed, PackageProblem? Failure);
}
