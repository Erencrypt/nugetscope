using NugetScope.Core.Analysis;

namespace NugetScope.Core.Reporting;

/// <param name="FailOn">The statuses that fail the run.</param>
/// <param name="Matches">Packages with at least one of those statuses.</param>
/// <param name="Gaps">Why the data may be incomplete; a run with gaps never passes.</param>
public sealed record FailureEvaluation(
    PackageStatus FailOn,
    IReadOnlyList<PackageSummary> Matches,
    IReadOnlyList<string> Gaps)
{
    public bool Failed => Matches.Count > 0;

    public bool Passed => !Failed && Gaps.Count == 0;

    /// <summary>One line describing the outcome, for logs.</summary>
    public string Describe()
    {
        string statuses = string.Join(", ", PackageStatusNames.ToNames(FailOn));

        if (Failed)
        {
            return $"FAILED: {Matches.Count} package(s) match --fail-on {statuses}.";
        }

        return Gaps.Count > 0
            ? $"INCOMPLETE: no package matches --fail-on {statuses}, but the data is incomplete."
            : $"PASSED: no package matches --fail-on {statuses}.";
    }
}
