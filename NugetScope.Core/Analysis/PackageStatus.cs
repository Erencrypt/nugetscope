namespace NugetScope.Core.Analysis;

/// <summary>Issues found for a package. <see cref="None"/> only means "up to date" for the checks that completed.</summary>
[Flags]
public enum PackageStatus
{
    None = 0,
    Outdated = 1,
    Deprecated = 2,
    Vulnerable = 4,
    Mismatch = 8,

    /// <summary>Best-effort guess that a direct reference isn't used by its project's code. Not counted as an issue.</summary>
    PossiblyUnused = 16,
}
