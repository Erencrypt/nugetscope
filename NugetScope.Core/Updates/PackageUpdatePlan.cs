namespace NugetScope.Core.Updates;

/// <summary>Exactly what an update will change, for confirmation before anything is written.</summary>
/// <param name="Edits">The file edits to apply.</param>
/// <param name="Notes">Projects or declarations that will not change, and why.</param>
public sealed record PackageUpdatePlan(
    string PackageId,
    string TargetVersion,
    IReadOnlyList<PlannedEdit> Edits,
    IReadOnlyList<string> Notes)
{
    public bool HasChanges => Edits.Count > 0;
}
