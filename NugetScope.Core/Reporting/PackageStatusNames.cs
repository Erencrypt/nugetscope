using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;

namespace NugetScope.Core.Reporting;

/// <summary>Stable lower-case names for statuses and checks, shared by reports and the <c>--fail-on</c> option.</summary>
public static class PackageStatusNames
{
    public const string Any = "any";

    // Most severe first: the order used in reports. Only issues can gate a run; "possibly unused" is just a hint.
    private static readonly (PackageStatus Status, string Name, PackageChecks Check, bool IsIssue)[] Entries =
    [
        (PackageStatus.Vulnerable, "vulnerable", PackageChecks.Vulnerable, true),
        (PackageStatus.Deprecated, "deprecated", PackageChecks.Deprecated, true),
        (PackageStatus.Outdated, "outdated", PackageChecks.Outdated, true),
        (PackageStatus.Mismatch, "mismatch", PackageChecks.None, true),
        (PackageStatus.PossiblyUnused, "possibly-unused", PackageChecks.None, false),
    ];

    /// <summary>Every status that counts as an issue (what <c>--fail-on any</c> means).</summary>
    public static PackageStatus AnyIssue { get; } =
        Entries.Where(e => e.IsIssue).Aggregate(PackageStatus.None, (all, e) => all | e.Status);

    /// <summary>Each single status set in <paramref name="status"/>, most severe first.</summary>
    public static IEnumerable<PackageStatus> Each(PackageStatus status) =>
        Entries.Where(e => status.HasFlag(e.Status)).Select(e => e.Status);

    public static IReadOnlyList<string> ToNames(PackageStatus status) => Each(status).Select(Name).ToList();

    public static string Name(PackageStatus single) => Entries.First(e => e.Status == single).Name;

    /// <summary>The check a status depends on; <see cref="PackageChecks.None"/> for mismatch, which needs no feed.</summary>
    public static PackageChecks RequiredCheck(PackageStatus single) => Entries.First(e => e.Status == single).Check;

    public static IReadOnlyList<string> ToNames(PackageChecks checks) =>
        Entries.Where(e => e.Check != PackageChecks.None && checks.HasFlag(e.Check)).Select(e => e.Name).ToList();

    /// <summary>Parses a comma-separated list such as <c>vulnerable,outdated</c> or <c>any</c>.</summary>
    public static bool TryParse(string text, out PackageStatus status)
    {
        status = PackageStatus.None;

        foreach (string part in text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.Equals(Any, StringComparison.OrdinalIgnoreCase))
            {
                status |= AnyIssue;
                continue;
            }

            int index = Array.FindIndex(Entries, e => e.IsIssue && e.Name.Equals(part, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                status = PackageStatus.None;
                return false;
            }

            status |= Entries[index].Status;
        }

        return status != PackageStatus.None;
    }
}
