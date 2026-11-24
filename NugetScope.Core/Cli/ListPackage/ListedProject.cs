using NugetScope.Core.Packages;

namespace NugetScope.Core.Cli.ListPackage;

/// <summary>One project from <c>dotnet list package</c> output.</summary>
internal sealed record ListedProject(
    string Path,
    IReadOnlyList<PackageReference> Packages,
    IReadOnlyList<PackageProblem> Problems);
