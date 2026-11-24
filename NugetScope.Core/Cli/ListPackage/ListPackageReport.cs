using NugetScope.Core.Packages;

namespace NugetScope.Core.Cli.ListPackage;

/// <summary>Parsed <c>dotnet list package</c> output, keyed by normalized project path.</summary>
internal sealed record ListPackageReport(
    IReadOnlyDictionary<string, ListedProject> Projects,
    IReadOnlyList<PackageProblem> Problems);
