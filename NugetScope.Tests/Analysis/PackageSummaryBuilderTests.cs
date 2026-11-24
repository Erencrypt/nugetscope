using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Core.Solutions;

namespace NugetScope.Tests.Analysis;

public sealed class PackageSummaryBuilderTests
{
    private static readonly ProjectInfo Api = new("Api", "/repo/Api/Api.csproj", null);
    private static readonly ProjectInfo Lib = new("Lib", "/repo/Lib/Lib.csproj", null);

    private static PackageReference Ref(
        string id,
        string resolved,
        string framework = "net8.0",
        bool transitive = false,
        bool autoReferenced = false) => new()
    {
        Id = id,
        TargetFramework = framework,
        RequestedVersion = transitive ? null : resolved,
        ResolvedVersion = resolved,
        IsTransitive = transitive,
        IsAutoReferenced = autoReferenced,
    };

    private static PackageScanResult Scan(params (ProjectInfo Project, PackageReference[] Packages)[] projects) =>
        new(projects.Select(p => new ProjectPackages(p.Project, p.Packages, [])).ToList(), []);

    [Fact]
    public void Build_GroupsByPackageIdAcrossProjects_CaseInsensitive()
    {
        var scan = Scan(
            (Api, [Ref("Serilog", "4.0.0")]),
            (Lib, [Ref("serilog", "4.0.0")]));

        var summary = Assert.Single(PackageSummaryBuilder.Build(scan));

        Assert.Equal("Serilog", summary.Id);
        Assert.Equal(2, summary.ProjectCount);
        Assert.Equal(["4.0.0"], summary.ResolvedVersions);
        Assert.False(summary.HasVersionMismatch);
    }

    [Fact]
    public void Build_DifferentVersions_FlagsMismatchAndSortsSemantically()
    {
        var scan = Scan(
            (Api, [Ref("Polly", "10.0.0")]),
            (Lib, [Ref("Polly", "9.1.0")]));

        var summary = Assert.Single(PackageSummaryBuilder.Build(scan));

        Assert.Equal(["9.1.0", "10.0.0"], summary.ResolvedVersions);
        Assert.True(summary.HasVersionMismatch);
    }

    [Fact]
    public void Build_MultiTargeted_MergesFrameworksOnlyWhenVersionsMatch()
    {
        var scan = Scan((Api,
        [
            Ref("Same", "8.0.0", "net8.0"), Ref("Same", "8.0.0", "netstandard2.0"),
            Ref("Split", "8.0.0", "net8.0"), Ref("Split", "7.2.4", "netstandard2.0"),
        ]));

        var summaries = PackageSummaryBuilder.Build(scan);

        var same = Assert.Single(summaries.Single(s => s.Id == "Same").Usages);
        Assert.Equal(["net8.0", "netstandard2.0"], same.TargetFrameworks);

        var split = summaries.Single(s => s.Id == "Split");
        Assert.Equal(["7.2.4", "8.0.0"], split.Usages.Select(u => u.ResolvedVersion));
        Assert.Equal(1, split.ProjectCount);
    }

    [Fact]
    public void Build_ExcludesTransitiveAndAutoReferencedByDefault()
    {
        var scan = Scan((Api,
        [
            Ref("Serilog", "4.0.0"),
            Ref("System.Memory", "4.5.5", transitive: true),
            Ref("NETStandard.Library", "2.0.3", autoReferenced: true),
        ]));

        Assert.Equal(["Serilog"], PackageSummaryBuilder.Build(scan).Select(s => s.Id));
        Assert.Equal(
            ["NETStandard.Library", "Serilog", "System.Memory"],
            PackageSummaryBuilder.Build(scan, includeTransitive: true, includeAutoReferenced: true).Select(s => s.Id));
    }

    [Fact]
    public void Build_SortsPackagesByIdAndUsagesByProject()
    {
        var scan = Scan(
            (Lib, [Ref("Zeta", "1.0.0"), Ref("alpha", "1.0.0")]),
            (Api, [Ref("Zeta", "1.0.0")]));

        var summaries = PackageSummaryBuilder.Build(scan);

        Assert.Equal(["alpha", "Zeta"], summaries.Select(s => s.Id));
        Assert.Equal(["Api", "Lib"], summaries[1].Usages.Select(u => u.Project.Name));
    }
}
