using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;

namespace NugetScope.Tui.Rows;

/// <summary>
/// One row of the package table, in either grouping. Rows are classes (reference identity) because the tree
/// table tracks expansion per object, so children are built once and reused.
/// </summary>
internal abstract class DashboardRow
{
    public abstract string Name { get; }

    /// <summary>Versions in use, lowest first; empty for rows that group other rows.</summary>
    public abstract IReadOnlyList<string> ResolvedVersions { get; }

    public abstract string? LatestVersion { get; }

    public abstract PackageStatus Status { get; }

    public abstract VulnerabilitySeverity? HighestSeverity { get; }

    /// <summary>Projects using a package, or packages in a project; null when not meaningful.</summary>
    public abstract int? Count { get; }

    public virtual IReadOnlyList<DashboardRow> Children => [];

    /// <summary>The package this row describes, for the detail pane; null for project rows.</summary>
    public abstract PackageSummary? Package { get; }
}
