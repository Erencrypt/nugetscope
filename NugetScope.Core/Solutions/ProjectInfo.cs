namespace NugetScope.Core.Solutions;

/// <summary>A project entry from a solution file.</summary>
/// <param name="Name">Display name as shown in the solution.</param>
/// <param name="FilePath">Absolute path to the project file.</param>
/// <param name="SolutionFolder">Solution folder path such as <c>src/libs</c>, or null at the root.</param>
public sealed record ProjectInfo(string Name, string FilePath, string? SolutionFolder);
