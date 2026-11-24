namespace NugetScope.Core.Analysis;

/// <summary>Text search on package id combined with a status filter.</summary>
public sealed record PackageFilter(string? SearchText = null, PackageStatusFilter Status = PackageStatusFilter.All)
{
    public static PackageFilter None { get; } = new();

    public bool IsActive => !string.IsNullOrWhiteSpace(SearchText) || Status != PackageStatusFilter.All;

    public IReadOnlyList<PackageSummary> Apply(IEnumerable<PackageSummary> packages) => packages.Where(Matches).ToList();

    public bool Matches(PackageSummary package) => MatchesText(package) && MatchesStatus(package.Status);

    private bool MatchesText(PackageSummary package) =>
        string.IsNullOrWhiteSpace(SearchText)
        || package.Id.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase);

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
