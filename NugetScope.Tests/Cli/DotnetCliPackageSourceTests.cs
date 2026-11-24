using System.ComponentModel;
using NugetScope.Core.Cli;
using NugetScope.Core.Packages;
using NugetScope.Core.Solutions;
using NugetScope.Tests.Support;

namespace NugetScope.Tests.Cli;

public sealed class DotnetCliPackageSourceTests
{
    private const string EmptyListJson = """{ "version": 1, "projects": [] }""";

    private static readonly SolutionInfo Solution = new(
        FilePath: Path.GetFullPath("C:/repo/Fx.slnx"),
        Projects:
        [
            Project("A"),
            Project("B"),
            Project("C"),
            Project("D"), // In the solution but absent from the CLI output.
        ]);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static ProjectInfo Project(string name) => new(name, FixtureFile.RepoProject(name), null);

    private static ProcessResult Ok(string stdout) => new(0, stdout, "");

    private static ProcessResult Fixture(string name, int exitCode = 0) => new(exitCode, FixtureFile.Read(name), "");

    /// <summary>Captured restore + no-restore list output; status checks answer "nothing flagged".</summary>
    private static FakeProcessRunner RunnerWithCapturedOutput() => new FakeProcessRunner()
        .OnRestore(Fixture("DotnetRestore/restore-output.txt", exitCode: 1))
        .OnList(Fixture("DotnetList/project-problems.json", exitCode: 1))
        .OnList("--outdated", Ok(EmptyListJson))
        .OnList("--deprecated", Ok(EmptyListJson))
        .OnList("--vulnerable", Ok(EmptyListJson));

    private static Task<PackageScanResult> ScanAsync(FakeProcessRunner runner, PackageScanOptions? options = null) =>
        new DotnetCliPackageSource(runner).ScanAsync(Solution, options ?? PackageScanOptions.Default, cancellationToken: Ct);

    [Fact]
    public async Task ScanAsync_RestoresOnceThenRunsBaseAndStatusListsWithoutRestoring()
    {
        var runner = RunnerWithCapturedOutput();

        await ScanAsync(runner, new PackageScanOptions(IncludeTransitive: true));

        Assert.Equal("restore", runner.Calls[0][0]);
        var lists = runner.Calls.Skip(1).ToList();
        Assert.Equal(4, lists.Count);
        Assert.All(lists, call =>
        {
            Assert.Equal("list", call[0]);
            Assert.Equal(Solution.FilePath, call[1]);
            Assert.Contains("--no-restore", call);
            Assert.Contains("--include-transitive", call);
        });
        Assert.Contains(lists, c => c.Contains("--outdated"));
        Assert.Contains(lists, c => c.Contains("--deprecated"));
        Assert.Contains(lists, c => c.Contains("--vulnerable"));
    }

    [Fact]
    public async Task ScanAsync_NoChecksAndNoRestore_RunsOnlyBaseList()
    {
        var runner = RunnerWithCapturedOutput();

        var result = await ScanAsync(runner, new PackageScanOptions(Restore: false, Checks: PackageChecks.None));

        var call = Assert.Single(runner.Calls);
        Assert.Equal("list", call[0]);
        Assert.DoesNotContain("--include-transitive", call);
        Assert.Equal(PackageChecks.None, result.CompletedChecks);
    }

    [Fact]
    public async Task ScanAsync_ReturnsOneEntryPerSolutionProjectInOrder()
    {
        var result = await ScanAsync(RunnerWithCapturedOutput());

        Assert.Equal(["A", "B", "C", "D"], result.Projects.Select(p => p.Project.Name));
        Assert.Same(Solution.Projects[0], result.Projects[0].Project);
    }

    [Fact]
    public async Task ScanAsync_AttachesListProblemsAndRestoreErrorsToProjects()
    {
        var result = await ScanAsync(RunnerWithCapturedOutput());

        var a = result.Projects[0];
        Assert.False(a.HasErrors);
        Assert.Contains(a.Packages, p => p.Id == "Newtonsoft.Json");

        Assert.Contains(result.Projects[1].Problems, p => p.Message.Contains("No assets file"));
        Assert.Contains(result.Projects[2].Problems, p => p.Code == "NU1101");
        Assert.Equal(ProblemLevel.Warning, Assert.Single(result.Projects[3].Problems).Level);
        Assert.Empty(result.Problems);
    }

    [Fact]
    public async Task ScanAsync_MergesStatusDataAndReportsAllChecksCompleted()
    {
        var runner = new FakeProcessRunner()
            .OnRestore(Ok(""))
            .OnList(Fixture("DotnetList/transitive.json"))
            .OnList("--outdated", Fixture("DotnetList/outdated.json"))
            .OnList("--deprecated", Fixture("DotnetList/deprecated.json"))
            .OnList("--vulnerable", Fixture("DotnetList/vulnerable.json"));

        var result = await ScanAsync(runner);

        var a = result.Projects[0].Packages;
        Assert.Equal("4.4.0", a.First(p => p.Id == "Serilog").LatestVersion);
        Assert.NotNull(a.First(p => p.Id == "WindowsAzure.Storage").Deprecation);
        Assert.All(a.Where(p => p.Id == "Newtonsoft.Json"), p => Assert.NotEmpty(p.Vulnerabilities));
        Assert.Equal(PackageChecks.All, result.CompletedChecks);
    }

    [Fact]
    public async Task ScanAsync_StatusCheckWithUnreadableOutput_IsSkippedWithWarning()
    {
        var runner = RunnerWithCapturedOutput()
            .OnList("--vulnerable", new ProcessResult(1, "", "Unable to load the service index"));

        var result = await ScanAsync(runner);

        Assert.Equal(PackageChecks.Outdated | PackageChecks.Deprecated, result.CompletedChecks);
        var problem = Assert.Single(result.Problems);
        Assert.Equal(ProblemLevel.Warning, problem.Level);
        Assert.Contains("vulnerable", problem.Message);
        Assert.NotEmpty(result.Projects[0].Packages);
    }

    [Fact]
    public async Task ScanAsync_StatusCheckWithSolutionLevelError_IsNotCompleted()
    {
        const string feedError = """
            { "version": 1, "problems": [ { "level": "error", "text": "Unable to load the service index for source https://example.invalid." } ], "projects": [] }
            """;
        var runner = RunnerWithCapturedOutput().OnList("--outdated", Ok(feedError));

        var result = await ScanAsync(runner);

        Assert.False(result.CompletedChecks.HasFlag(PackageChecks.Outdated));
        Assert.Contains(result.Problems, p => p.Message.Contains("service index"));
    }

    [Fact]
    public async Task ScanAsync_DuplicateProblemsAcrossCalls_AreReportedOnce()
    {
        var problems = Fixture("DotnetList/project-problems.json", exitCode: 1);
        var runner = RunnerWithCapturedOutput()
            .OnList("--outdated", problems)
            .OnList("--deprecated", problems)
            .OnList("--vulnerable", problems);

        var result = await ScanAsync(runner);

        Assert.Single(result.Projects[1].Problems, p => p.Message.Contains("No assets file"));
    }

    [Fact]
    public async Task ScanAsync_RestoreErrorWithoutProject_IsSolutionProblem()
    {
        var runner = RunnerWithCapturedOutput()
            .OnRestore(Ok("error NU1301: Unable to load the service index for source https://example.invalid/v3/index.json."));

        var result = await ScanAsync(runner);

        Assert.Equal("NU1301", Assert.Single(result.Problems).Code);
    }

    [Fact]
    public async Task ScanAsync_UnreadableBaseList_ThrowsWithStandardError()
    {
        var runner = RunnerWithCapturedOutput()
            .OnList(new ProcessResult(1, "", "MSBUILD : error MSB1009: Project file does not exist."));

        var ex = await Assert.ThrowsAsync<PackageSourceException>(() => ScanAsync(runner));

        Assert.Contains("MSB1009", ex.Message);
    }

    [Fact]
    public async Task ScanAsync_DotnetNotInstalled_ThrowsPackageSourceException()
    {
        var runner = new FakeProcessRunner().OnRestore(() => throw new Win32Exception(2, "File not found"));

        var ex = await Assert.ThrowsAsync<PackageSourceException>(() => ScanAsync(runner));

        Assert.IsType<Win32Exception>(ex.InnerException);
    }
}
