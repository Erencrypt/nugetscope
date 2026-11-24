namespace NugetScope.Core.Solutions;

/// <summary>A parsed solution and the .NET projects it contains.</summary>
/// <param name="FilePath">Absolute path to the .sln or .slnx file.</param>
/// <param name="Projects">Projects that can carry NuGet package references.</param>
public sealed record SolutionInfo(string FilePath, IReadOnlyList<ProjectInfo> Projects)
{
    public string Name => Path.GetFileNameWithoutExtension(FilePath);

    public string DirectoryPath => Path.GetDirectoryName(FilePath)!;
}
