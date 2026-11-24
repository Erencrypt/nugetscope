using NugetScope.Core.Cli.Restore;
using NugetScope.Core.Packages;
using NugetScope.Tests.Support;

namespace NugetScope.Tests.Cli;

public sealed class RestoreOutputParserTests
{
    [Fact]
    public void Parse_CapturedOutput_ReturnsErrorsAndWarningsPerProject()
    {
        var diagnostics = RestoreOutputParser.Parse(FixtureFile.Read("DotnetRestore/restore-output.txt"));

        Assert.Equal(2, diagnostics.Count);

        var error = Assert.Single(diagnostics, d => d.Level == ProblemLevel.Error);
        Assert.Equal(FixtureFile.RepoProject("C"), error.FilePath);
        Assert.Equal("NU1101", error.Code);
        Assert.StartsWith("Unable to find package Nugetscope.Does.Not.Exist.Xyz.", error.Message);
        Assert.DoesNotContain("Fx.slnx", error.Message);

        var warning = Assert.Single(diagnostics, d => d.Level == ProblemLevel.Warning);
        Assert.Equal(FixtureFile.RepoProject("A"), warning.FilePath);
        Assert.Equal("NU1903", warning.Code);
    }

    [Fact]
    public void Parse_LineWithoutFile_HasNullPath()
    {
        var diagnostic = Assert.Single(RestoreOutputParser.Parse("error NU1301: Unable to load the service index."));

        Assert.Null(diagnostic.FilePath);
        Assert.Equal("NU1301", diagnostic.Code);
    }

    [Fact]
    public void Parse_IgnoresNonDiagnosticLinesAndDuplicates()
    {
        const string line = @"C:\repo\C\C.csproj : error NU1101: Unable to find package X.";
        string output = $"  Determining projects to restore...\r\n{line}\r\n{line}\r\n  Restored C:\\repo\\A\\A.csproj (in 1 sec).";

        Assert.Single(RestoreOutputParser.Parse(output));
    }
}
