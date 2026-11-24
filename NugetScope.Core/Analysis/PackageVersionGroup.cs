using NugetScope.Core.Solutions;

namespace NugetScope.Core.Analysis;

/// <summary>Projects that resolve a package to the same version.</summary>
public sealed record PackageVersionGroup(string Version, IReadOnlyList<ProjectInfo> Projects);
