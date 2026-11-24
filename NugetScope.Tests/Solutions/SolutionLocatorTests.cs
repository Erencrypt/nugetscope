using NugetScope.Core.Solutions;
using NugetScope.Tests.Support;

namespace NugetScope.Tests.Solutions;

public sealed class SolutionLocatorTests
{
    [Fact]
    public void FindInDirectory_ReturnsSlnAndSlnxOnly()
    {
        using var temp = new TempDirectory();
        temp.CreateFile("B.slnx", "<Solution />");
        temp.CreateFile("A.sln", "");
        temp.CreateFile("readme.md", "");
        temp.CreateFile("C.sln.bak", "");

        var found = SolutionLocator.FindInDirectory(temp.Path).Select(Path.GetFileName);

        Assert.Equal(["A.sln", "B.slnx"], found);
    }

    [Fact]
    public void Resolve_NoArgument_SingleSolution_IsFound()
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
    public void Resolve_RelativeDirectoryArgument_SearchesThatDirectory()
    {
        using var temp = new TempDirectory();
        string solution = temp.CreateFile(Path.Combine("src", "App.slnx"), "<Solution />");

        var result = SolutionLocator.Resolve("src", temp.Path);

        Assert.Equal(solution, Assert.IsType<SolutionResolution.Found>(result).SolutionPath);
    }

    [Theory]
    [InlineData("App.csproj", "Not a solution file")]
    [InlineData("missing.sln", "Path not found")]
    public void Resolve_InvalidArgument_IsNotFoundWithReason(string argument, string expectedMessage)
    {
        using var temp = new TempDirectory();
        temp.CreateFile("App.csproj", "<Project />");

        var result = SolutionLocator.Resolve(argument, temp.Path);

        Assert.Contains(expectedMessage, Assert.IsType<SolutionResolution.NotFound>(result).Message);
    }
}
