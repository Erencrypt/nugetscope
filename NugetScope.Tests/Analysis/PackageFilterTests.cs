using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Core.Solutions;

namespace NugetScope.Tests.Analysis;

public sealed class PackageFilterTests
{
    private static readonly ProjectInfo Api = new("Api", "/repo/Api/Api.csproj", null);
    private static readonly ProjectInfo Lib = new("Lib", "/repo/Lib/Lib.csproj", null);

    private static PackageUsage Usage(ProjectInfo project, string version) => new(project, ["net8.0"], version, version, false);

    private static readonly PackageSummary Clean = new("Serilog", [Usage(Api, "4.0.0")]);

    private static readonly PackageSummary Outdated = new("Polly", [Usage(Api, "7.0.0") with { LatestVersion = "8.0.0" }]);

    private static readonly PackageSummary Vulnerable = new("Newtonsoft.Json",
        [Usage(Api, "12.0.1") with { Vulnerabilities = [new PackageVulnerability(VulnerabilitySeverity.High, null)] }]);

    private static readonly PackageSummary Deprecated = new("WindowsAzure.Storage",
        [Usage(Api, "9.3.3") with { Deprecation = new PackageDeprecation(["Legacy"], null, null) }]);

    private static readonly PackageSummary Mismatched = new("Humanizer", [Usage(Api, "2.0.0"), Usage(Lib, "2.14.1")]);

    private static readonly PackageSummary[] All = [Clean, Outdated, Vulnerable, Deprecated, Mismatched];

    [Theory]
    [InlineData(PackageStatusFilter.All, new[] { "Serilog", "Polly", "Newtonsoft.Json", "WindowsAzure.Storage", "Humanizer" })]
    [InlineData(PackageStatusFilter.AnyIssue, new[] { "Polly", "Newtonsoft.Json", "WindowsAzure.Storage", "Humanizer" })]
    [InlineData(PackageStatusFilter.Outdated, new[] { "Polly" })]
    [InlineData(PackageStatusFilter.Vulnerable, new[] { "Newtonsoft.Json" })]
    [InlineData(PackageStatusFilter.Deprecated, new[] { "WindowsAzure.Storage" })]
    [InlineData(PackageStatusFilter.Mismatch, new[] { "Humanizer" })]
    public void Apply_StatusFilter(PackageStatusFilter status, string[] expected)
    {
        var result = new PackageFilter(Status: status).Apply(All);

        Assert.Equal(expected, result.Select(p => p.Id));
    }

    [Theory]
    [InlineData("json", new[] { "Newtonsoft.Json" })]
    [InlineData("  SERI ", new[] { "Serilog" })]
    [InlineData("", new[] { "Serilog", "Polly", "Newtonsoft.Json", "WindowsAzure.Storage", "Humanizer" })]
    [InlineData("nothing-matches", new string[0])]
    public void Apply_TextSearch_IsCaseInsensitiveContainsOnId(string search, string[] expected)
    {
        var result = new PackageFilter(SearchText: search).Apply(All);

        Assert.Equal(expected, result.Select(p => p.Id));
    }

    [Fact]
    public void Apply_CombinesTextAndStatus()
    {
        var result = new PackageFilter("o", PackageStatusFilter.AnyIssue).Apply(All);

        Assert.Equal(["Polly", "Newtonsoft.Json", "WindowsAzure.Storage"], result.Select(p => p.Id));
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
