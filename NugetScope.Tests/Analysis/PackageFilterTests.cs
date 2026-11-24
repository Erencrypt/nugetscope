using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Core.Solutions;

namespace NugetScope.Tests.Analysis;

public sealed class PackageFilterTests
{
    private static readonly ProjectInfo Api = new("Api", "/repo/Api/Api.csproj", null);
    private static readonly ProjectInfo Lib = new("Lib", "/repo/Lib/Lib.csproj", null);

    private static PackageUsage Usage(ProjectInfo project, string version) => new(project, ["net8.0"], version, version, false);

    private static readonly PackageSummary[] All =
    [
        new("Serilog", [Usage(Api, "4.0.0")]),
        new("Polly", [Usage(Api, "7.0.0") with { LatestVersion = "8.0.0" }]),
        new("Newtonsoft.Json", [Usage(Api, "12.0.1") with { Vulnerabilities = [new PackageVulnerability(VulnerabilitySeverity.High, null)] }]),
        new("WindowsAzure.Storage", [Usage(Api, "9.3.3") with { Deprecation = new PackageDeprecation(["Legacy"], null, null) }]),
        new("Humanizer", [Usage(Api, "2.0.0"), Usage(Lib, "2.14.1")]),
    ];

    [Theory]
    [InlineData(PackageStatusFilter.All, new[] { "Serilog", "Polly", "Newtonsoft.Json", "WindowsAzure.Storage", "Humanizer" })]
    [InlineData(PackageStatusFilter.AnyIssue, new[] { "Polly", "Newtonsoft.Json", "WindowsAzure.Storage", "Humanizer" })]
    [InlineData(PackageStatusFilter.Vulnerable, new[] { "Newtonsoft.Json" })]
    [InlineData(PackageStatusFilter.Mismatch, new[] { "Humanizer" })]
    public void Apply_StatusFilter(PackageStatusFilter status, string[] expected)
    {
        Assert.Equal(expected, new PackageFilter(Status: status).Apply(All).Select(p => p.Id));
    }

    [Theory]
    [InlineData("  SERI ", new[] { "Serilog" })]
    [InlineData("nothing-matches", new string[0])]
    public void Apply_TextSearch_IsTrimmedCaseInsensitiveContainsOnId(string search, string[] expected)
    {
        Assert.Equal(expected, new PackageFilter(SearchText: search).Apply(All).Select(p => p.Id));
    }

    [Fact]
    public void Apply_CombinesTextAndStatus()
    {
        var result = new PackageFilter("o", PackageStatusFilter.AnyIssue).Apply(All);

        Assert.Equal(["Polly", "Newtonsoft.Json", "WindowsAzure.Storage"], result.Select(p => p.Id));
    }

    [Fact]
    public void Apply_Projects_FiltersPackagesByOwnStatus_AndDropsEmptyProjects()
    {
        var projects = ProjectSummaryBuilder.Build(All);

        var result = new PackageFilter(Status: PackageStatusFilter.Mismatch).Apply(projects);

        Assert.Equal(["Api", "Lib"], result.Select(p => p.Project.Name));
        Assert.All(result, p => Assert.Equal(["Humanizer"], p.Packages.Select(x => x.Package.Id)));
        Assert.DoesNotContain(new PackageFilter(Status: PackageStatusFilter.Vulnerable).Apply(projects), p => p.Project == Lib);
    }

    [Fact]
    public void Apply_Projects_SearchMatchesProjectNameOrPackageId()
    {
        var projects = ProjectSummaryBuilder.Build(All);

        Assert.Equal(["Humanizer"], new PackageFilter("lib").Apply(projects).Single().Packages.Select(p => p.Package.Id));
        Assert.Equal(["Api"], new PackageFilter("polly").Apply(projects).Select(p => p.Project.Name));
    }

    [Fact]
    public void IsActive_OnlyWhenSearchOrStatusSet()
    {
        Assert.False(PackageFilter.None.IsActive);
        Assert.False(new PackageFilter("   ").IsActive);
        Assert.True(new PackageFilter("x").IsActive);
        Assert.True(new PackageFilter(Status: PackageStatusFilter.Outdated).IsActive);
    }
}
