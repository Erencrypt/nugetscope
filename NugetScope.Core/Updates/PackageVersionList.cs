using NugetScope.Core.Packages;

namespace NugetScope.Core.Updates;

/// <param name="Versions">Distinct versions available from the configured sources, newest first.</param>
/// <param name="Problems">Sources that could not be searched.</param>
public sealed record PackageVersionList(IReadOnlyList<string> Versions, IReadOnlyList<PackageProblem> Problems);
