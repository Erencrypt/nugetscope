using System.ComponentModel;
using NugetScope.Core.Cli.ListPackage;
using NugetScope.Core.Cli.Restore;
using NugetScope.Core.IO;
using NugetScope.Core.Packages;
using NugetScope.Core.Solutions;

namespace NugetScope.Core.Cli;

/// <summary>
/// Package data via the dotnet CLI: one <c>dotnet restore</c>, then one solution-wide
/// <c>dotnet list package --no-restore</c>. With <c>--no-restore</c>, a broken project is reported
/// in <c>problems</c> instead of failing the whole call, so healthy projects still return data.
/// </summary>
public sealed class DotnetCliPackageSource(IProcessRunner processRunner) : IPackageSource
{
    private const string DotnetExecutable = "dotnet";

    public async Task<PackageScanResult> ScanAsync(
        SolutionInfo solution,
        PackageScanOptions options,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RestoreDiagnostic> restoreDiagnostics = options.Restore
            ? await RestoreAsync(solution, cancellationToken).ConfigureAwait(false)
            : [];

        ProcessResult listResult = await RunDotnetAsync(BuildListArguments(solution, options), solution, cancellationToken)
            .ConfigureAwait(false);

        ListPackageReport report;
        try
        {
            // Exit code 1 is normal when any project has a problem; the JSON is still complete.
            report = ListPackageParser.Parse(listResult.StandardOutput);
        }
        catch (FormatException ex)
        {
            throw new PackageSourceException(
                $"Could not read 'dotnet list package' output (exit code {listResult.ExitCode}). {listResult.StandardError}".TrimEnd(),
                ex);
        }

        return Merge(solution, report, restoreDiagnostics);
    }

    internal static IReadOnlyList<string> BuildListArguments(SolutionInfo solution, PackageScanOptions options)
    {
        var arguments = new List<string>
        {
            "list", solution.FilePath, "package",
            "--format", "json",
            "--output-version", ListPackageParser.SupportedOutputVersion.ToString(),
            "--no-restore",
        };

        if (options.IncludeTransitive)
        {
            arguments.Add("--include-transitive");
        }

        return arguments;
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

    private async Task<ProcessResult> RunDotnetAsync(
        IReadOnlyList<string> arguments,
        SolutionInfo solution,
        CancellationToken cancellationToken)
    {
        try
        {
            return await processRunner.RunAsync(DotnetExecutable, arguments, solution.DirectoryPath, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Win32Exception ex)
        {
            throw new PackageSourceException("Could not start the dotnet CLI. Is the .NET SDK installed and on PATH?", ex);
        }
    }

    private static PackageScanResult Merge(
        SolutionInfo solution,
        ListPackageReport report,
        IReadOnlyList<RestoreDiagnostic> restoreDiagnostics)
    {
        var restoreErrorsByProject = restoreDiagnostics
            .Where(d => d.Level == ProblemLevel.Error && d.FilePath is not null)
            .ToLookup(d => d.FilePath!, PathNormalizer.Comparer);

        var solutionProjectPaths = solution.Projects
            .Select(p => PathNormalizer.Normalize(p.FilePath))
            .ToHashSet(PathNormalizer.Comparer);

        var projects = solution.Projects.Select(project =>
        {
            string path = PathNormalizer.Normalize(project.FilePath);
            var problems = restoreErrorsByProject[path].Select(d => d.ToProblem()).ToList();

            if (report.Projects.TryGetValue(path, out ListedProject? listed))
            {
                problems.AddRange(listed.Problems);
                return new ProjectPackages(project, listed.Packages, problems);
            }

            if (problems.Count == 0)
            {
                problems.Add(new PackageProblem(ProblemLevel.Warning, "Project was not reported by 'dotnet list package'."));
            }

            return new ProjectPackages(project, [], problems);
        }).ToList();

        // Restore errors not tied to a solution project (feed/auth errors, the .sln itself) are solution-wide.
        var solutionProblems = report.Problems
            .Concat(restoreDiagnostics
                .Where(d => d.Level == ProblemLevel.Error && (d.FilePath is null || !solutionProjectPaths.Contains(d.FilePath)))
                .Select(d => d.ToProblem()))
            .ToList();

        return new PackageScanResult(projects, solutionProblems);
    }
}
