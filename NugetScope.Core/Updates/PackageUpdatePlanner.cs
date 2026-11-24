using NuGet.Versioning;
using NugetScope.Core.Analysis;
using NugetScope.Core.IO;
using NugetScope.Core.Packages;
using NugetScope.Core.Solutions;

namespace NugetScope.Core.Updates;

/// <summary>
/// Works out which files an update changes: evaluates every project that uses the package to find where its
/// version is declared (csproj, <c>VersionOverride</c>, or a central <c>PackageVersion</c>), then locates the
/// exact value in each file. Reads only; nothing is written.
/// </summary>
public sealed class PackageUpdatePlanner(IProjectEvaluator evaluator)
{
    private const int MaxParallelEvaluations = 4;

    /// <param name="projects">Projects to update; others are evaluated too, to list everyone a shared version affects.</param>
    /// <exception cref="ArgumentException"><paramref name="targetVersion"/> is not a valid NuGet version.</exception>
    public async Task<PackageUpdatePlan> PlanAsync(
        PackageSummary package,
        IReadOnlyCollection<ProjectInfo> projects,
        string targetVersion,
        CancellationToken cancellationToken = default)
    {
        if (!NuGetVersion.TryParse(targetVersion, out _))
        {
            throw new ArgumentException($"'{targetVersion}' is not a valid package version.", nameof(targetVersion));
        }

        using var throttle = new SemaphoreSlim(MaxParallelEvaluations);
        ProjectDeclarations[] located = await Task.WhenAll(package.Usages
                .Where(u => !u.IsTransitive)
                .GroupBy(u => u.Project)
                .Select(g => LocateAsync(
                    package.Id,
                    g.Key,
                    g.SelectMany(u => u.TargetFrameworks).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    throttle,
                    cancellationToken)))
            .ConfigureAwait(false);

        var selected = located.Where(l => projects.Contains(l.Project)).ToList();
        var notes = selected.Where(l => l.Problem is not null).Select(l => $"{l.Project.Name}: {l.Problem}").ToList();
        var edits = new List<PlannedEdit>();

        foreach (VersionDeclaration declaration in selected.SelectMany(l => l.Declarations).Distinct())
        {
            var affected = located.Where(l => l.Declarations.Contains(declaration)).Select(l => l.Project).ToList();
            string fileName = Path.GetFileName(declaration.FilePath);

            try
            {
                VersionLocation location = ProjectFileVersionFinder.Find(
                    TextFile.Read(declaration.FilePath).Text, declaration.Kind, package.Id);

                if (location.Value.Contains("$(", StringComparison.Ordinal))
                {
                    notes.Add($"{fileName}: the version comes from an MSBuild property ({location.Value}); change that property by hand.");
                }
                else if (string.Equals(location.Value, targetVersion, StringComparison.OrdinalIgnoreCase))
                {
                    notes.Add($"{fileName}: already {targetVersion}.");
                }
                else
                {
                    edits.Add(new PlannedEdit(declaration, location.Value, targetVersion, affected));
                }
            }
            catch (Exception ex) when (ex is PackageUpdateException or IOException or UnauthorizedAccessException)
            {
                notes.Add($"{fileName}: {ex.Message}");
            }
        }

        return new PackageUpdatePlan(package.Id, targetVersion, edits, notes);
    }

    private async Task<ProjectDeclarations> LocateAsync(
        string packageId,
        ProjectInfo project,
        IReadOnlyList<string> targetFrameworks,
        SemaphoreSlim throttle,
        CancellationToken cancellationToken)
    {
        var declarations = new HashSet<VersionDeclaration>();

        // Multi-targeted projects can declare versions per framework, so evaluate each framework in use.
        IReadOnlyList<string?> frameworks = targetFrameworks.Count > 0 ? [.. targetFrameworks] : [null];
        foreach (string? framework in frameworks)
        {
            ProjectEvaluation evaluation;
            await throttle.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                evaluation = await evaluator.EvaluateAsync(project.FilePath, framework, cancellationToken).ConfigureAwait(false);
            }
            catch (PackageSourceException ex)
            {
                return new ProjectDeclarations(project, [], ex.Message);
            }
            finally
            {
                throttle.Release();
            }

            if (evaluation.FindDeclaration(packageId) is { } declaration)
            {
                declarations.Add(declaration);
            }
        }

        return declarations.Count == 0
            ? new ProjectDeclarations(project, [], $"no version for {packageId} was found in the project's MSBuild files.")
            : new ProjectDeclarations(project, declarations, null);
    }

    private sealed record ProjectDeclarations(ProjectInfo Project, IReadOnlyCollection<VersionDeclaration> Declarations, string? Problem);
}
