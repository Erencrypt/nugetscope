using NugetScope.Core.Packages;
using NugetScope.Core.Solutions;

namespace NugetScope.Core.Scanning;

/// <summary>Everything known about a solution after one scan.</summary>
public sealed record SolutionScan(SolutionInfo Solution, PackageScanResult Packages);
