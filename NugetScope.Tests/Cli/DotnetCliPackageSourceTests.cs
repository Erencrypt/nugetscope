using System.ComponentModel;
using NugetScope.Core.Cli;
using NugetScope.Core.Packages;
using NugetScope.Core.Solutions;
using NugetScope.Tests.Support;

namespace NugetScope.Tests.Cli;

public sealed class DotnetCliPackageSourceTests
{
    private static readonly SolutionInfo Solution = new(
        FilePath: Path.GetFullPath("C:/repo/Fx.slnx"),
        Projects:
        [
            Project("A"),
            Project("B"),
            Project("C"),
            Project("D"), // In the solution but absent from the CLI output.
        ]);

    private static ProjectInfo Project(string name) => new(name, FixtureFile.RepoProject(name), null);

    private static ProcessResult Ok(string stdout) => new(0, stdout, "");

    private static FakeProcessRunner RunnerWithCapturedOutput() => new FakeProcessRunner()
        .On("restore", new ProcessResult(1, FixtureFile.Read("DotnetRestore/restore-output.txt"), ""))
        .On("list", new ProcessResult(1, FixtureFile.Read("DotnetList/project-problems.json"), ""));

    [Fact]
    public async Task ScanAsync_RestoresThenListsWithoutRestoring()
    {
        var runner = RunnerWithCapturedOutput();

        await new DotnetCliPackageSource(runner).ScanAsync(Solution, new PackageScanOptions(IncludeTransitive: true), TestContext.Current.CancellationToken);

        Assert.Equal(["restore", "list"], runner.Calls.Select(c => c[0]));
        var list = runner.Calls[1];
        Assert.Equal(Solution.FilePath, list[1]);
        Assert.Contains("--no-restore", list);
        Assert.Contains("--include-transitive", list);
        Assert.Equal("json", list[list.ToList().IndexOf("--format") + 1]);
        Assert.Equal("1", list[list.ToList().IndexOf("--output-version") + 1]);
    }

    [Fact]
    public async Task ScanAsync_RestoreDisabled_SkipsRestore()
    {
        var runner = RunnerWithCapturedOutput();

        await new DotnetCliPackageSource(runner).ScanAsync(Solution, new PackageScanOptions(Restore: false), TestContext.Current.CancellationToken);

        var call = Assert.Single(runner.Calls);
        Assert.Equal("list", call[0]);
        Assert.DoesNotContain("--include-transitive", call);
    }

    [Fact]
    public async Task ScanAsync_ReturnsOneEntryPerSolutionProjectInOrder()
    {
        var result = await new DotnetCliPackageSource(RunnerWithCapturedOutput()).ScanAsync(Solution, PackageScanOptions.Default, TestContext.Current.CancellationToken);

        Assert.Equal(["A", "B", "C", "D"], result.Projects.Select(p => p.Project.Name));
        Assert.Same(Solution.Projects[0], result.Projects[0].Project);
    }

    [Fact]
    public async Task ScanAsync_HealthyProject_HasPackagesAndNoErrors()
    {
        var result = await new DotnetCliPackageSource(RunnerWithCapturedOutput()).ScanAsync(Solution, PackageScanOptions.Default, TestContext.Current.CancellationToken);

        var a = result.Projects[0];
        Assert.False(a.HasErrors);
        Assert.Contains(a.Packages, p => p.Id == "Newtonsoft.Json");
        // Restore warnings (NU1903) are not attached; vulnerability data comes from list --vulnerable.
        Assert.Empty(a.Problems);
    }

    [Fact]
    public async Task ScanAsync_AttachesListProblemsAndRestoreErrorsToProjects()
    {
        var result = await new DotnetCliPackageSource(RunnerWithCapturedOutput()).ScanAsync(Solution, PackageScanOptions.Default, TestContext.Current.CancellationToken);

        var b = result.Projects[1];
        Assert.True(b.HasErrors);
        Assert.Contains(b.Problems, p => p.Message.Contains("No assets file"));

        var c = result.Projects[2];
        Assert.True(c.HasErrors);
        Assert.Contains(c.Problems, p => p.Code == "NU1101");

        Assert.Empty(result.Problems);
    }

    [Fact]
    public async Task ScanAsync_ProjectMissingFromOutput_GetsWarning()
    {
        var result = await new DotnetCliPackageSource(RunnerWithCapturedOutput()).ScanAsync(Solution, PackageScanOptions.Default, TestContext.Current.CancellationToken);

        var d = result.Projects[3];
        Assert.Empty(d.Packages);
        Assert.Equal(ProblemLevel.Warning, Assert.Single(d.Problems).Level);
    }

    [Fact]
    public async Task ScanAsync_RestoreErrorWithoutProject_IsSolutionProblem()
    {
        var runner = new FakeProcessRunner()
            .On("restore", Ok("error NU1301: Unable to load the service index for source https://example.invalid/v3/index.json."))
            .On("list", Ok(FixtureFile.Read("DotnetList/transitive.json")));

        var result = await new DotnetCliPackageSource(runner).ScanAsync(Solution, PackageScanOptions.Default, TestContext.Current.CancellationToken);

        Assert.Equal("NU1301", Assert.Single(result.Problems).Code);
    }

    [Fact]
    public async Task ScanAsync_UnreadableListOutput_ThrowsWithStandardError()
    {
        var runner = new FakeProcessRunner()
            .On("restore", Ok(""))
            .On("list", new ProcessResult(1, "", "MSBUILD : error MSB1009: Project file does not exist."));

        var ex = await Assert.ThrowsAsync<PackageSourceException>(
            () => new DotnetCliPackageSource(runner).ScanAsync(Solution, PackageScanOptions.Default, TestContext.Current.CancellationToken));

        Assert.Contains("MSB1009", ex.Message);
    }

    [Fact]
    public async Task ScanAsync_DotnetNotInstalled_ThrowsPackageSourceException()
    {
        var runner = new FakeProcessRunner().On("restore", () => throw new Win32Exception(2, "File not found"));

        var ex = await Assert.ThrowsAsync<PackageSourceException>(
            () => new DotnetCliPackageSource(runner).ScanAsync(Solution, PackageScanOptions.Default, TestContext.Current.CancellationToken));

        Assert.IsType<Win32Exception>(ex.InnerException);
    }
}
