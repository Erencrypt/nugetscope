using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Core.Solutions;

namespace NugetScope.Tests.Analysis;

public sealed class ProjectSummaryBuilderTests
{
    private static readonly ProjectInfo Api = new("Api", "/repo/Api/Api.csproj", null);
    private static readonly ProjectInfo Lib = new("lib", "/repo/Lib/Lib.csproj", null);

    private static PackageUsage Usage(ProjectInfo project, string version, string framework = "net8.0") =>
        new(project, [framework], version, version, false);

    [Fact]
    public void Build_GroupsPackagesPerProject_SortedByName()
    {
        PackageSummary[] packages =
        [
            new("Polly", [Usage(Api, "8.0.0"), Usage(Lib, "8.0.0")]),
            new("Serilog", [Usage(Lib, "4.0.0")]),
        ];

        var projects = ProjectSummaryBuilder.Build(packages);

        Assert.Equal(["Api", "lib"], projects.Select(p => p.Project.Name));
        Assert.Equal(["Polly"], projects[0].Packages.Select(p => p.Package.Id));
        Assert.Equal(["Polly", "Serilog"], projects[1].Packages.Select(p => p.Package.Id));
    }

    [Fact]
    public void Build_StatusIsPerProject_ExceptMismatchWhichIsSolutionWide()
    {
        PackageSummary[] packages =
        [
            new("Polly", [Usage(Api, "7.0.0") with { LatestVersion = "8.0.0" }, Usage(Lib, "8.0.0", "net8.0"), Usage(Lib, "7.0.0", "netstandard2.0")]),
        ];

        var projects = ProjectSummaryBuilder.Build(packages);

        var api = Assert.Single(projects[0].Packages);
        Assert.Equal(PackageStatus.Outdated | PackageStatus.Mismatch, api.Status);

        var lib = Assert.Single(projects[1].Packages);
        Assert.Equal(["7.0.0", "8.0.0"], lib.ResolvedVersions);
        Assert.Equal(PackageStatus.Mismatch, lib.Status);
    }
}
