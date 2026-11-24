using NugetScope.Core.IO;

namespace NugetScope.Tests.Support;

internal static class FixtureFile
{
    public static string Directory { get; } = Path.Combine(AppContext.BaseDirectory, "Fixtures");

    public static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(Directory, relativePath));

    /// <summary>Normalized path of a project inside the captured <c>C:/repo</c> fixture solution.</summary>
    public static string RepoProject(string name) =>
        PathNormalizer.Normalize($"C:/repo/{name}/{name}.csproj");
}
