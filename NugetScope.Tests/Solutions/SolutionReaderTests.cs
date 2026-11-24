using NugetScope.Core.Solutions;
using NugetScope.Tests.Support;

namespace NugetScope.Tests.Solutions;

public sealed class SolutionReaderTests
{
    private static readonly string FixturesDirectory = Path.Combine(FixtureFile.Directory, "Solutions");

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly SolutionReader _reader = new();

    [Theory]
    [InlineData("Sample.sln")]
    [InlineData("Sample.slnx")]
    public async Task ReadAsync_ReturnsDotnetProjectsWithFoldersAndFullPaths(string fileName)
    {
        var solution = await _reader.ReadAsync(Path.Combine(FixturesDirectory, fileName), Ct);

        // The fixture also contains a .sqlproj, which must be skipped.
        Assert.Equal(
            [("App", "src"), ("App.Tests", null), ("Lib", "src/libs")],
            solution.Projects.OrderBy(p => p.Name, StringComparer.Ordinal).Select(p => (p.Name, p.SolutionFolder)));

        var app = solution.Projects.Single(p => p.Name == "App");
        Assert.Equal(Path.Combine(FixturesDirectory, "src", "App", "App.csproj"), app.FilePath);
    }

    [Fact]
    public async Task ReadAsync_MissingFile_ThrowsSolutionLoadException()
    {
        string path = Path.Combine(FixturesDirectory, "DoesNotExist.slnx");

        var ex = await Assert.ThrowsAsync<SolutionLoadException>(() => _reader.ReadAsync(path, Ct));

        Assert.Equal(path, ex.SolutionPath);
    }

    [Fact]
    public async Task ReadAsync_MalformedSlnx_ThrowsSolutionLoadException()
    {
        using var temp = new TempDirectory();
        string path = temp.CreateFile("Broken.slnx", "<Solution><Project");

        var ex = await Assert.ThrowsAsync<SolutionLoadException>(() => _reader.ReadAsync(path, Ct));

        Assert.NotNull(ex.InnerException);
    }
}
