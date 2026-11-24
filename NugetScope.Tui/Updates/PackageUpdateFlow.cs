using NugetScope.Core.Analysis;
using NugetScope.Core.Solutions;
using NugetScope.Core.Updates;
using NugetScope.Tui.Dialogs;
using NugetScope.Tui.Formatting;
using NugetScope.Tui.Threading;
using Terminal.Gui.App;
using Terminal.Gui.Views;

namespace NugetScope.Tui.Updates;

/// <summary>
/// Update steps: pick a version, plan the file edits, confirm, apply. Nothing is written before the user
/// confirms the exact list of changes.
/// </summary>
internal sealed class PackageUpdateFlow(IApplication app, IPackageVersionSource versionSource, PackageUpdatePlanner planner)
{
    /// <summary>Call off the UI thread. Returns true when files were written (the caller rescans).</summary>
    /// <param name="setBusy">Shows a busy message in the UI, or clears it with null. Called on the UI thread.</param>
    public async Task<bool> RunAsync(
        PackageSummary package,
        IReadOnlyCollection<ProjectInfo> projects,
        string solutionDirectory,
        Action<string?> setBusy)
    {
        try
        {
            await app.InvokeAsync(() => setBusy($"Fetching versions of {package.Id}...")).ConfigureAwait(false);
            PackageVersionList versions = await versionSource.GetVersionsAsync(package.Id, solutionDirectory).ConfigureAwait(false);

            string? target = await app.InvokeAsync(() =>
            {
                setBusy(null);
                return PickVersion(package, projects, versions);
            }).ConfigureAwait(false);

            if (target is null)
            {
                return false;
            }

            await app.InvokeAsync(() => setBusy($"Finding where {package.Id} versions are set...")).ConfigureAwait(false);
            PackageUpdatePlan plan = await planner.PlanAsync(package, projects, target).ConfigureAwait(false);

            bool confirmed = await app.InvokeAsync(() =>
            {
                setBusy(null);
                return Confirm(plan, solutionDirectory);
            }).ConfigureAwait(false);

            if (!confirmed)
            {
                return false;
            }

            PackageUpdater.Apply(plan);
            return true;
        }
        catch (Exception ex)
        {
            // Includes PackageSourceException/PackageUpdateException; never let a background flow fail silently.
            await app.InvokeAsync(() =>
            {
                setBusy(null);
                MessageBox.ErrorQuery(app, "Update failed", ex.Message, "_OK");
            }).ConfigureAwait(false);
            return false;
        }
    }

    private string? PickVersion(PackageSummary package, IReadOnlyCollection<ProjectInfo> projects, PackageVersionList versions)
    {
        string problems = string.Join("\n", versions.Problems.Select(p => p.Message));

        if (versions.Versions.Count == 0)
        {
            MessageBox.ErrorQuery(app, $"Update {package.Id}", $"No versions of {package.Id} were found.\n{problems}".TrimEnd(), "_OK");
            return null;
        }

        string? latest = UpdateFormatter.LatestFor(package, projects);
        string description = UpdateFormatter.DescribeScope(package, projects)
            + (problems.Length > 0 ? $"\nSome sources failed:\n{problems}" : "");

        using var dialog = new VersionPickerDialog(
            package.Id,
            description,
            versions.Versions,
            UpdateFormatter.VersionLabels(versions.Versions, package.ResolvedVersions, latest),
            UpdateFormatter.DefaultVersionIndex(versions.Versions, latest));

        app.Run(dialog);
        return dialog.Result;
    }

    private bool Confirm(PackageUpdatePlan plan, string solutionDirectory)
    {
        string title = $"Update {plan.PackageId}";
        string text = UpdateFormatter.FormatPlan(plan, solutionDirectory);

        if (!plan.HasChanges)
        {
            MessageBox.Query(app, title, text, "_OK");
            return false;
        }

        // Cancel is last, so it is the default button: Enter alone never writes files.
        return MessageBox.Query(app, title, text, "_Update", "_Cancel") == 0;
    }
}
