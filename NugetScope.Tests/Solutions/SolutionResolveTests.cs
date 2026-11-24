using NugetScope.Core.Solutions;
using NugetScope.Tests.Support;

namespace NugetScope.Tests.Solutions;

public sealed class SolutionResolveTests
{
    [Fact]
    public void Resolve_NoArgument_SingleSolutionInCurrentDirectory_IsFound()
    {
        using var temp = new TempDirectory();
        string solution = temp.CreateFile("App.slnx", "<Solution />");

        var result = SolutionLocator.Resolve(null, temp.Path);

        Assert.Equal(solution, Assert.IsType<SolutionResolution.Found>(result).SolutionPath);
    }

    [Fact]
    public void Resolve_NoArgument_SeveralSolutions_IsAmbiguous()
    {
        using var temp = new TempDirectory();
        temp.CreateFile("A.sln", "");
        temp.CreateFile("B.slnx", "<Solution />");

        var result = SolutionLocator.Resolve(null, temp.Path);

        Assert.Equal(2, Assert.IsType<SolutionResolution.Ambiguous>(result).Candidates.Count);
    }

    [Fact]
    public void Resolve_NoArgument_NoSolutions_IsNotFound()
    {
        using var temp = new TempDirectory();

        Assert.IsType<SolutionResolution.NotFound>(SolutionLocator.Resolve(null, temp.Path));
    }

    [Fact]
    public void Resolve_RelativeSolutionFileArgument_IsFoundAsFullPath()
    {
        using var temp = new TempDirectory();
        string solution = temp.CreateFile(Path.Combine("src", "App.sln"), "");

        var result = SolutionLocator.Resolve(Path.Combine("src", "App.sln"), temp.Path);

        Assert.Equal(solution, Assert.IsType<SolutionResolution.Found>(result).SolutionPath);
    }

    [Fact]
    public void Resolve_DirectoryArgument_SearchesThatDirectory()
    {
        using var temp = new TempDirectory();
        string solution = temp.CreateFile(Path.Combine("src", "App.slnx"), "<Solution />");

        var result = SolutionLocator.Resolve("src", temp.Path);

        Assert.Equal(solution, Assert.IsType<SolutionResolution.Found>(result).SolutionPath);
    }

    [Fact]
    public void Resolve_NonSolutionFile_IsNotFound()
    {
        using var temp = new TempDirectory();
        temp.CreateFile("App.csproj", "<Project />");

        var result = SolutionLocator.Resolve("App.csproj", temp.Path);

        Assert.Contains("Not a solution file", Assert.IsType<SolutionResolution.NotFound>(result).Message);
    }

    [Fact]
    public void Resolve_MissingPath_IsNotFound()
    {
        using var temp = new TempDirectory();

        var result = SolutionLocator.Resolve("nope.sln", temp.Path);

        Assert.Contains("Path not found", Assert.IsType<SolutionResolution.NotFound>(result).Message);
    }
}
