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
    public void FindInDirectory_NoSolutions_ReturnsEmpty()
    {
        using var temp = new TempDirectory();
        temp.CreateFile("readme.md", "");

        Assert.Empty(SolutionLocator.FindInDirectory(temp.Path));
    }

    [Fact]
    public void FindInDirectory_MissingDirectory_ReturnsEmpty()
    {
        string missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        Assert.Empty(SolutionLocator.FindInDirectory(missing));
    }
}
