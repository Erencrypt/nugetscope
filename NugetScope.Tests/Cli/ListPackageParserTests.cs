using NugetScope.Core.Cli.ListPackage;
using NugetScope.Core.Packages;
using NugetScope.Tests.Support;

namespace NugetScope.Tests.Cli;

// Status fields (latest/deprecation/vulnerabilities) are covered via ListPackageReportMergerTests,
// which parse the captured status fixtures.
public sealed class ListPackageParserTests
{
    private static ListPackageReport ParseFixture(string name) =>
        ListPackageParser.Parse(FixtureFile.Read(Path.Combine("DotnetList", name)));

    private static IReadOnlyList<PackageReference> PackagesOf(ListPackageReport report, string project) =>
        report.Projects[FixtureFile.RepoProject(project)].Packages;

    [Fact]
    public void Parse_MultiTargetedProject_ReturnsPackagesPerFramework()
    {
        var packages = PackagesOf(ParseFixture("transitive.json"), "A");

        Assert.Equal("net8.0", Assert.Single(packages, p => p.Id == "Humanizer.Core").TargetFramework);
        Assert.Equal(["netstandard2.0", "net8.0"], packages.Where(p => p.Id == "Newtonsoft.Json").Select(p => p.TargetFramework));
    }

    [Fact]
    public void Parse_KeepsFloatingVersionsAndRangesAsWritten()
    {
        var packages = PackagesOf(ParseFixture("transitive.json"), "A");

        var serilog = packages.First(p => p.Id == "Serilog");
        Assert.Equal("3.*", serilog.RequestedVersion);
        Assert.Equal("3.1.1", serilog.ResolvedVersion);
        Assert.Equal("[2.14.1]", packages.First(p => p.Id == "Humanizer.Core").RequestedVersion);
    }

    [Fact]
    public void Parse_FlagsTransitiveAndAutoReferencedPackages()
    {
        var packages = PackagesOf(ParseFixture("transitive.json"), "A");

        Assert.True(Assert.Single(packages, p => p.Id == "NETStandard.Library" && !p.IsTransitive).IsAutoReferenced);

        var transitive = packages.Where(p => p.IsTransitive).ToList();
        Assert.NotEmpty(transitive);
        Assert.All(transitive, p =>
        {
            Assert.Null(p.RequestedVersion);
            Assert.NotNull(p.ResolvedVersion);
        });
    }

    [Fact]
    public void Parse_ProjectProblem_AttachesToProjectAndKeepsOtherProjects()
    {
        var report = ParseFixture("project-problems.json");

        var broken = report.Projects[FixtureFile.RepoProject("B")];
        var problem = Assert.Single(broken.Problems);
        Assert.Equal(ProblemLevel.Error, problem.Level);
        Assert.Contains("No assets file", problem.Message);
        Assert.Empty(broken.Packages);

        Assert.NotEmpty(PackagesOf(report, "A"));
        Assert.Empty(report.Problems);
    }

    [Fact]
    public void Parse_TopLevelProblemOnly_ReturnsSolutionProblem()
    {
        var report = ParseFixture("restore-failed.json");

        Assert.Empty(report.Projects);
        Assert.StartsWith("Restore failed", Assert.Single(report.Problems).Message);
    }

    [Fact]
    public void Parse_IgnoresTextBeforeJson()
    {
        var report = ListPackageParser.Parse("Some SDK notice\n" + FixtureFile.Read("DotnetList/restore-failed.json"));

        Assert.Single(report.Problems);
    }

    [Theory]
    [InlineData("error: something went wrong")]
    [InlineData("{ \"version\": 1, \"projects\": [ ")]
    [InlineData("{ \"version\": 2, \"projects\": [] }")]
    public void Parse_InvalidOrUnsupportedOutput_ThrowsFormatException(string output)
    {
        Assert.Throws<FormatException>(() => ListPackageParser.Parse(output));
    }
}
