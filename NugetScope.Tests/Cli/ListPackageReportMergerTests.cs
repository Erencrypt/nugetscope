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
    public void Merge_AppliesStatusFieldsPerProjectAndFramework()
    {
        var merged = MergeAllStatuses();
        var a = PackagesOf(merged, "A");

        Assert.All(a.Where(p => p.Id == "Serilog" && !p.IsTransitive), p => Assert.Equal("4.4.0", p.LatestVersion));
        Assert.Equal("13.0.4", PackagesOf(merged, "B").Single(p => p.Id == "Newtonsoft.Json").LatestVersion);

        Assert.All(a.Where(p => p.Id == "WindowsAzure.Storage"), p =>
        {
            Assert.Equal(["Legacy"], p.Deprecation?.Reasons);
            Assert.Equal("Azure.Storage.Common", p.Deprecation?.AlternativePackageId);
        });

        Assert.All(a.Where(p => p.Id == "Newtonsoft.Json"), p =>
        {
            var vulnerability = Assert.Single(p.Vulnerabilities);
            Assert.Equal(VulnerabilitySeverity.High, vulnerability.Severity);
            Assert.Equal(new Uri("https://github.com/advisories/GHSA-5crp-9r3c-p9vr"), vulnerability.AdvisoryUrl);
        });
    }

    [Fact]
    public void Merge_LeavesUnflaggedPackagesAndTransitivesUntouched()
    {
        var merged = PackagesOf(MergeAllStatuses(), "A");

        Assert.Equal(PackagesOf(Parse("transitive.json"), "A").Count, merged.Count);

        var humanizer = merged.Single(p => p.Id == "Humanizer.Core");
        Assert.Null(humanizer.Deprecation);
        Assert.Empty(humanizer.Vulnerabilities);
        Assert.All(merged.Where(p => p.IsTransitive), p => Assert.Null(p.LatestVersion));
    }

    [Fact]
    public void Merge_DeduplicatesProblemsRepeatedByEveryCall()
    {
        var problems = Parse("project-problems.json");

        var merged = ListPackageReportMerger.Merge(problems, [problems, problems]);

        Assert.Single(merged.Projects[FixtureFile.RepoProject("B")].Problems);
    }
}
