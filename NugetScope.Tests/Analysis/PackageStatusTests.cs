using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Core.Solutions;

namespace NugetScope.Tests.Analysis;

public sealed class PackageStatusTests
{
    private static readonly ProjectInfo Api = new("Api", "/repo/Api/Api.csproj", null);
    private static readonly ProjectInfo Lib = new("Lib", "/repo/Lib/Lib.csproj", null);

    private static readonly PackageVulnerability High = new(VulnerabilitySeverity.High, new Uri("https://example.test/high"));
    private static readonly PackageVulnerability Low = new(VulnerabilitySeverity.Low, new Uri("https://example.test/low"));

    private static PackageReference Ref(string id, string resolved) => new()
    {
        Id = id,
        TargetFramework = "net8.0",
        RequestedVersion = resolved,
        ResolvedVersion = resolved,
    };

    private static PackageSummary Summarize(params (ProjectInfo Project, PackageReference Package)[] references)
    {
        var scan = new PackageScanResult(
            references.GroupBy(r => r.Project)
                .Select(g => new ProjectPackages(g.Key, g.Select(r => r.Package).ToList(), []))
                .ToList(),
            [],
            PackageChecks.All);

        return Assert.Single(PackageSummaryBuilder.Build(scan));
    }

    [Fact]
    public void NoStatusData_IsNone()
    {
        var summary = Summarize((Api, Ref("Serilog", "4.0.0")));

        Assert.Equal(PackageStatus.None, summary.Status);
        Assert.Null(summary.LatestVersion);
        Assert.Null(summary.HighestSeverity);
    }

    [Fact]
    public void Outdated_UsesHighestLatestVersionAndFlagsOnlyOutdatedUsages()
    {
        var summary = Summarize(
            (Api, Ref("Serilog", "3.1.1") with { LatestVersion = "4.4.0" }),
            (Lib, Ref("Serilog", "4.4.0")));

        Assert.True(summary.Status.HasFlag(PackageStatus.Outdated));
        Assert.Equal("4.4.0", summary.LatestVersion);
        Assert.True(summary.Usages.Single(u => u.Project == Api).IsOutdated);
        Assert.False(summary.Usages.Single(u => u.Project == Lib).IsOutdated);
    }

    [Fact]
    public void Vulnerable_CollectsDistinctAdvisoriesMostSevereFirst()
    {
        var summary = Summarize(
            (Api, Ref("Newtonsoft.Json", "12.0.1") with { Vulnerabilities = [Low, High] }),
            (Lib, Ref("Newtonsoft.Json", "12.0.1") with { Vulnerabilities = [High] }));

        Assert.True(summary.Status.HasFlag(PackageStatus.Vulnerable));
        Assert.Equal([High, Low], summary.Vulnerabilities);
        Assert.Equal(VulnerabilitySeverity.High, summary.HighestSeverity);
    }

    [Fact]
    public void Deprecated_IsFlaggedWithDeprecationInfo()
    {
        var deprecation = new PackageDeprecation(["Legacy"], "Azure.Storage.Blobs", ">= 0.0.0");

        var summary = Summarize((Api, Ref("WindowsAzure.Storage", "9.3.3") with { Deprecation = deprecation }));

        Assert.Equal(PackageStatus.Deprecated, summary.Status);
        Assert.Same(deprecation, summary.Deprecation);
    }

    [Fact]
    public void CombinesAllFlags()
    {
        var summary = Summarize(
            (Api, Ref("Old", "1.0.0") with
            {
                LatestVersion = "3.0.0",
                Vulnerabilities = [High],
                Deprecation = new PackageDeprecation(["Legacy"], null, null),
            }),
            (Lib, Ref("Old", "2.0.0")));

        Assert.Equal(
            PackageStatus.Outdated | PackageStatus.Vulnerable | PackageStatus.Deprecated | PackageStatus.Mismatch,
            summary.Status);
    }
}
