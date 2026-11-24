using NugetScope.Core.Cli.ListPackage;
using NugetScope.Core.Packages;
using NugetScope.Tests.Support;

namespace NugetScope.Tests.Cli;

public sealed class ListPackageReportMergerTests
{
    private static ListPackageReport Parse(string name) =>
        ListPackageParser.Parse(FixtureFile.Read(Path.Combine("DotnetList", name)));

    private static IReadOnlyList<PackageReference> PackagesOf(ListPackageReport report, string project) =>
        report.Projects[FixtureFile.RepoProject(project)].Packages;

    private static ListPackageReport MergeAllStatuses() => ListPackageReportMerger.Merge(
        Parse("transitive.json"),
        [Parse("outdated.json"), Parse("deprecated.json"), Parse("vulnerable.json")]);

    [Fact]
    public void Merge_AppliesLatestVersionPerFramework()
    {
        var packages = PackagesOf(MergeAllStatuses(), "A");

        Assert.All(packages.Where(p => p.Id == "Serilog" && !p.IsTransitive), p => Assert.Equal("4.4.0", p.LatestVersion));
        Assert.Equal("13.0.4", PackagesOf(MergeAllStatuses(), "B").Single(p => p.Id == "Newtonsoft.Json").LatestVersion);
    }

    [Fact]
    public void Merge_AppliesDeprecationAndVulnerabilities()
    {
        var packages = PackagesOf(MergeAllStatuses(), "A");

        Assert.All(packages.Where(p => p.Id == "WindowsAzure.Storage"), p => Assert.Equal("Azure.Storage.Common", p.Deprecation?.AlternativePackageId));
        Assert.All(packages.Where(p => p.Id == "Newtonsoft.Json"), p =>
            Assert.Equal(VulnerabilitySeverity.High, Assert.Single(p.Vulnerabilities).Severity));
    }

    [Fact]
    public void Merge_LeavesUnflaggedPackagesAndTransitivesUntouched()
    {
        var merged = MergeAllStatuses();
        var basePackages = PackagesOf(Parse("transitive.json"), "A");

        Assert.Equal(basePackages.Count, PackagesOf(merged, "A").Count);

        var humanizer = PackagesOf(merged, "A").Single(p => p.Id == "Humanizer.Core");
        Assert.Null(humanizer.Deprecation);
        Assert.Empty(humanizer.Vulnerabilities);
        Assert.All(PackagesOf(merged, "A").Where(p => p.IsTransitive), p => Assert.Null(p.LatestVersion));
    }

    [Fact]
    public void Merge_NoStatusReports_ReturnsBaseData()
    {
        var baseReport = Parse("transitive.json");

        var merged = ListPackageReportMerger.Merge(baseReport, []);

        Assert.Equal(PackagesOf(baseReport, "A"), PackagesOf(merged, "A"));
    }

    [Fact]
    public void Merge_DeduplicatesRepeatedProblems()
    {
        var problems = Parse("project-problems.json");

        var merged = ListPackageReportMerger.Merge(problems, [problems, problems]);

        Assert.Single(merged.Projects[FixtureFile.RepoProject("B")].Problems);
    }
}
