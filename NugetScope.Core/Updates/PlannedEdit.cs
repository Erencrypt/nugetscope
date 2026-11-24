using NugetScope.Core.Solutions;

namespace NugetScope.Core.Updates;

/// <summary>One version value to rewrite in one file.</summary>
/// <param name="OldValue">The value exactly as written in the file (may be a range or floating version).</param>
/// <param name="AffectedProjects">Every project that takes its version from this declaration, selected or not.</param>
public sealed record PlannedEdit(
    VersionDeclaration Declaration,
    string OldValue,
    string NewValue,
    IReadOnlyList<ProjectInfo> AffectedProjects);
