namespace NugetScope.Core.Solutions;

/// <summary>Outcome of turning a command-line argument (or the current directory) into a solution file.</summary>
public abstract record SolutionResolution
{
    private SolutionResolution()
    {
    }

    public sealed record Found(string SolutionPath) : SolutionResolution;

    /// <summary>Several solutions were found; the user has to pick one.</summary>
    public sealed record Ambiguous(IReadOnlyList<string> Candidates) : SolutionResolution;

    public sealed record NotFound(string Message) : SolutionResolution;
}
