namespace NugetScope.Core.Packages;

/// <summary>Network-backed checks a scan can run in addition to listing packages.</summary>
[Flags]
public enum PackageChecks
{
    None = 0,
    Outdated = 1,
    Deprecated = 2,
    Vulnerable = 4,
    All = Outdated | Deprecated | Vulnerable,
}
