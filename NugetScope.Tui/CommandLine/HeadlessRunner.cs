using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Core.Reporting;
using NugetScope.Core.Scanning;
using NugetScope.Core.Solutions;

namespace NugetScope.Tui.CommandLine;

/// <summary>CI mode: scan once, print a report, and turn <c>--fail-on</c> into an exit code. Reports go to stdout, messages to stderr.</summary>
internal static class HeadlessRunner
{
    public static async Task<int> RunAsync(
        SolutionScanner scanner,
        string solutionPath,
        CommandLineOptions options,
        CancellationToken cancellationToken)
    {
        SolutionScan scan;
        try
        {
            scan = await scanner.ScanAsync(
                    solutionPath,
                    PackageScanOptions.Default with { IncludeTransitive = options.IncludeTransitive },
                    progress: null,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is SolutionLoadException or PackageSourceException)
        {
            await Console.Error.WriteLineAsync($"nugetscope: {ex.Message}").ConfigureAwait(false);
            return ExitCodes.Error;
        }

        var packages = PackageSummaryBuilder.Build(scan.Packages, includeTransitive: options.IncludeTransitive);
        FailureEvaluation? evaluation = options.FailOn == PackageStatus.None
            ? null
            : FailurePolicy.Evaluate(options.FailOn, packages, scan.Packages);

        if (options.Json)
        {
            await Console.Out.WriteLineAsync(JsonReportWriter.Write(scan, packages)).ConfigureAwait(false);

            // Keep stdout pure JSON; the gate result goes to stderr.
            if (evaluation is not null)
            {
                foreach (string line in evaluation.Gaps.Append(evaluation.Describe()))
                {
                    await Console.Error.WriteLineAsync(line).ConfigureAwait(false);
                }
            }
        }
        else
        {
            await Console.Out.WriteAsync(TextReportWriter.Write(scan, packages, evaluation)).ConfigureAwait(false);
        }

        return evaluation switch
        {
            { Failed: true } => ExitCodes.PolicyFailed,
            { Passed: false } => ExitCodes.Error,
            _ => ExitCodes.Success,
        };
    }
}
