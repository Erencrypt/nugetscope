namespace NugetScope.Core.Analysis;

/// <summary>Text search on package id combined with a status filter.</summary>
public sealed record PackageFilter(string? SearchText = null, PackageStatusFilter Status = PackageStatusFilter.All)
{
    public static PackageFilter None { get; } = new();

    public bool IsActive => !string.IsNullOrWhiteSpace(SearchText) || Status != PackageStatusFilter.All;

    public IReadOnlyList<PackageSummary> Apply(IEnumerable<PackageSummary> packages) => packages.Where(Matches).ToList();

    /// <summary>
    /// Keeps each project's packages whose own status matches and whose id (or the project's name) matches the search.
    /// Projects left without packages are dropped.
    /// </summary>
    public IReadOnlyList<ProjectSummary> Apply(IEnumerable<ProjectSummary> projects) => projects
        .Select(project =>
        {
            bool projectNameMatches = MatchesText(project.Project.Name);

            return project with
            {
                Packages = project.Packages
                    .Where(p => MatchesStatus(p.Status) && (projectNameMatches || MatchesText(p.Package.Id)))
                    .ToList(),
            };
        })
        .Where(project => project.Packages.Count > 0)
        .ToList();

    public bool Matches(PackageSummary package) => MatchesText(package.Id) && MatchesStatus(package.Status);

    private bool MatchesText(string text) =>
        string.IsNullOrWhiteSpace(SearchText)
        || text.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase);

    private bool MatchesStatus(PackageStatus status) => Status switch
    {
        PackageStatusFilter.All => true,
        PackageStatusFilter.AnyIssue => status != PackageStatus.None,
        PackageStatusFilter.Vulnerable => status.HasFlag(PackageStatus.Vulnerable),
        PackageStatusFilter.Outdated => status.HasFlag(PackageStatus.Outdated),
        PackageStatusFilter.Deprecated => status.HasFlag(PackageStatus.Deprecated),
        PackageStatusFilter.Mismatch => status.HasFlag(PackageStatus.Mismatch),
        _ => true,
    };
}
