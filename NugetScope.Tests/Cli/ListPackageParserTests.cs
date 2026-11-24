using NugetScope.Core.Cli.ListPackage;
using NugetScope.Core.Packages;
using NugetScope.Tests.Support;

namespace NugetScope.Tests.Cli;

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

        var humanizer = Assert.Single(packages, p => p.Id == "Humanizer.Core");
        Assert.Equal("net8.0", humanizer.TargetFramework);

        var newtonsoft = packages.Where(p => p.Id == "Newtonsoft.Json").Select(p => p.TargetFramework);
        Assert.Equal(["netstandard2.0", "net8.0"], newtonsoft);
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

        var autoRef = Assert.Single(packages, p => p.Id == "NETStandard.Library" && !p.IsTransitive);
        Assert.True(autoRef.IsAutoReferenced);

        var transitive = packages.Where(p => p.IsTransitive).ToList();
        Assert.NotEmpty(transitive);
        Assert.All(transitive, p => Assert.Null(p.RequestedVersion));
        Assert.All(transitive, p => Assert.NotNull(p.ResolvedVersion));
        Assert.DoesNotContain(packages, p => p.Id == "Newtonsoft.Json" && p.IsTransitive);
    }

    [Fact]
    public void Parse_Outdated_SetsLatestVersion()
    {
        var packages = PackagesOf(ParseFixture("outdated.json"), "A");

        Assert.Equal("4.4.0", packages.First(p => p.Id == "Serilog").LatestVersion);
    }

    [Fact]
    public void Parse_Deprecated_SetsReasonsAndAlternative()
    {
        var packages = PackagesOf(ParseFixture("deprecated.json"), "A");

        var deprecation = packages.First(p => p.Id == "WindowsAzure.Storage").Deprecation;
        Assert.NotNull(deprecation);
        Assert.Equal(["Legacy"], deprecation.Reasons);
        Assert.Equal("Azure.Storage.Common", deprecation.AlternativePackageId);
        Assert.Equal(">= 0.0.0", deprecation.AlternativeVersionRange);
    }

    [Fact]
    public void Parse_Vulnerable_SetsSeverityAndAdvisoryUrl()
    {
        var packages = PackagesOf(ParseFixture("vulnerable.json"), "A");

        var vulnerability = Assert.Single(packages.First(p => p.Id == "Newtonsoft.Json").Vulnerabilities);
        Assert.Equal(VulnerabilitySeverity.High, vulnerability.Severity);
        Assert.Equal(new Uri("https://github.com/advisories/GHSA-5crp-9r3c-p9vr"), vulnerability.AdvisoryUrl);
    }

    [Fact]
    public void Parse_PlainList_HasNoStatusData()
    {
        var packages = PackagesOf(ParseFixture("transitive.json"), "A");

        Assert.All(packages, p =>
        {
            Assert.Null(p.LatestVersion);
            Assert.Null(p.Deprecation);
            Assert.Empty(p.Vulnerabilities);
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

        Assert.NotEmpty(report.Projects[FixtureFile.RepoProject("A")].Packages);
        Assert.Empty(report.Problems);
    }

    [Fact]
    public void Parse_TopLevelProblemOnly_ReturnsSolutionProblem()
    {
        var report = ParseFixture("restore-failed.json");

        Assert.Empty(report.Projects);
        var problem = Assert.Single(report.Problems);
        Assert.Equal(ProblemLevel.Error, problem.Level);
        Assert.StartsWith("Restore failed", problem.Message);
    }

    [Fact]
    public void Parse_IgnoresTextBeforeJson()
    {
        var report = ListPackageParser.Parse("Some SDK notice\n" + FixtureFile.Read("DotnetList/restore-failed.json"));

        Assert.Single(report.Problems);
    }

    [Theory]
    [InlineData("")]
    [InlineData("error: something went wrong")]
    [InlineData("{ \"version\": 1, \"projects\": [ ")]
    public void Parse_InvalidOutput_ThrowsFormatException(string output)
    {
        Assert.Throws<FormatException>(() => ListPackageParser.Parse(output));
    }

    [Fact]
    public void Parse_UnsupportedVersion_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => ListPackageParser.Parse("{ \"version\": 2, \"projects\": [] }"));
    }
}
