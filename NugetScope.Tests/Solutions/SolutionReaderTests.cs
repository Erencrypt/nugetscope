using NugetScope.Core.Solutions;
using NugetScope.Tests.Support;

namespace NugetScope.Tests.Solutions;

public sealed class SolutionReaderTests
{
    private static readonly string FixturesDirectory =
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Solutions");

    private readonly SolutionReader _reader = new();

    [Theory]
    [InlineData("Sample.sln")]
    [InlineData("Sample.slnx")]
    public async Task ReadAsync_ReturnsDotnetProjectsWithFolders(string fileName)
    {
        var solution = await _reader.ReadAsync(Path.Combine(FixturesDirectory, fileName));

        var projects = solution.Projects
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .Select(p => (p.Name, p.SolutionFolder))
            .ToList();

        Assert.Equal(
            [("App", "src"), ("App.Tests", null), ("Lib", "src/libs")],
            projects);
    }

    [Theory]
    [InlineData("Sample.sln")]
    [InlineData("Sample.slnx")]
    public async Task ReadAsync_ResolvesProjectPathsRelativeToSolution(string fileName)
    {
        var solution = await _reader.ReadAsync(Path.Combine(FixturesDirectory, fileName));

        var app = Assert.Single(solution.Projects, p => p.Name == "App");
        Assert.Equal(Path.Combine(FixturesDirectory, "src", "App", "App.csproj"), app.FilePath);
    }

    [Fact]
    public async Task ReadAsync_ExposesSolutionNameAndDirectory()
    {
        var solution = await _reader.ReadAsync(Path.Combine(FixturesDirectory, "Sample.slnx"));

        Assert.Equal("Sample", solution.Name);
        Assert.Equal(FixturesDirectory, solution.DirectoryPath);
    }

    [Fact]
    public async Task ReadAsync_MissingFile_ThrowsSolutionLoadException()
    {
        string path = Path.Combine(FixturesDirectory, "DoesNotExist.slnx");

        var ex = await Assert.ThrowsAsync<SolutionLoadException>(() => _reader.ReadAsync(path));
        Assert.Equal(path, ex.SolutionPath);
    }

    [Fact]
    public async Task ReadAsync_UnsupportedExtension_ThrowsSolutionLoadException()
    {
        using var temp = new TempDirectory();
        string path = temp.CreateFile("NotASolution.txt", "hello");

        await Assert.ThrowsAsync<SolutionLoadException>(() => _reader.ReadAsync(path));
    }

    [Fact]
    public async Task ReadAsync_MalformedSlnx_ThrowsSolutionLoadException()
    {
        using var temp = new TempDirectory();
        string path = temp.CreateFile("Broken.slnx", "<Solution><Project");

        var ex = await Assert.ThrowsAsync<SolutionLoadException>(() => _reader.ReadAsync(path));
        Assert.NotNull(ex.InnerException);
    }
}
