using NugetScope.Core.Analysis;

namespace NugetScope.Tui.CommandLine;

/// <param name="SolutionArgument">Solution file or directory; null searches the current directory.</param>
/// <param name="Json">Print a JSON report instead of opening the dashboard.</param>
/// <param name="Markdown">Print a Markdown report instead of opening the dashboard.</param>
/// <param name="FailOn">Statuses that make the run exit with <see cref="ExitCodes.PolicyFailed"/>.</param>
/// <param name="IncludeTransitive">Include transitive packages.</param>
/// <param name="ShowHelp">Print usage and exit.</param>
internal sealed record CommandLineOptions(
    string? SolutionArgument = null,
    bool Json = false,
    bool Markdown = false,
    PackageStatus FailOn = PackageStatus.None,
    bool IncludeTransitive = false,
    bool ShowHelp = false)
{
    /// <summary>Run without the dashboard (CI mode).</summary>
    public bool Headless => Json || Markdown || FailOn != PackageStatus.None;
}
