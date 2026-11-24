using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Core.Scanning;
using NugetScope.Core.Solutions;
using NugetScope.Tui.Rows;

namespace NugetScope.Tui.State;

/// <summary>
/// Scan results and dashboard-wide settings shared by every page. Pages keep only their own filter and search.
/// Must be used on the UI thread.
/// </summary>
internal sealed class DashboardState
{
    private IReadOnlyDictionary<ProjectInfo, ProjectPackages> _projectScans = new Dictionary<ProjectInfo, ProjectPackages>();

    public event EventHandler? Changed;

    public SolutionScan? Scan { get; private set; }

    public IReadOnlyList<PackageSummary> Packages { get; private set; } = [];

    public IReadOnlyList<ProjectSummary> Projects { get; private set; } = [];

    public PackageChecks CompletedChecks { get; private set; }

    public PackageGrouping Grouping { get; private set; } = PackageGrouping.ByProject;

    /// <summary>Show packages that only come in through other packages. Needs a scan with transitive data.</summary>
    public bool ShowTransitive { get; private set; }

    /// <summary>Whether the current scan listed transitive packages (it only does when asked, as it is slower).</summary>
    public bool ScanHasTransitive { get; private set; }

    /// <param name="includesTransitive">The scan ran with transitive packages.</param>
    public void Load(SolutionScan scan, bool includesTransitive)
    {
        Scan = scan;
        ScanHasTransitive = includesTransitive;
        CompletedChecks = scan.Packages.CompletedChecks;
        _projectScans = scan.Packages.Projects.ToDictionary(p => p.Project);
        Rebuild();
    }

    /// <summary>Switches transitive packages on or off. Returns true when a rescan is needed to show them.</summary>
    public bool ToggleTransitive()
    {
        ShowTransitive = !ShowTransitive;

        if (ShowTransitive && !ScanHasTransitive)
        {
            return true;
        }

        if (Scan is not null)
        {
            Rebuild();
        }

        return false;
    }

    public void Clear()
    {
        Scan = null;
        ScanHasTransitive = false;
        Packages = [];
        Projects = [];
        CompletedChecks = PackageChecks.None;
        _projectScans = new Dictionary<ProjectInfo, ProjectPackages>();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void ToggleGrouping()
    {
        Grouping = Grouping == PackageGrouping.ByPackage ? PackageGrouping.ByProject : PackageGrouping.ByPackage;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Top-level rows for a filter in the current grouping.</summary>
    public IReadOnlyList<DashboardRow> RowsFor(PackageFilter filter) => Grouping switch
    {
        PackageGrouping.ByProject => filter.Apply(Projects).Select(p => (DashboardRow)new ProjectRow(p)).ToList(),
        _ => filter.Apply(Packages).Select(p => (DashboardRow)new PackageRow(p)).ToList(),
    };

    /// <summary>The project with all its packages, regardless of any page filter.</summary>
    public ProjectSummary? FindProject(ProjectInfo project) => Projects.FirstOrDefault(p => p.Project == project);

    public IReadOnlyList<PackageProblem> ProblemsFor(ProjectInfo project) =>
        _projectScans.TryGetValue(project, out ProjectPackages? scan) ? scan.Problems : [];

    private void Rebuild()
    {
        Packages = PackageSummaryBuilder.Build(Scan!.Packages, includeTransitive: ShowTransitive && ScanHasTransitive);
        Projects = ProjectSummaryBuilder.Build(Packages);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
