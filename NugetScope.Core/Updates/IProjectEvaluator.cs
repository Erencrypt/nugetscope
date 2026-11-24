namespace NugetScope.Core.Updates;

/// <summary>Evaluates a project file to find where its package versions are declared. Does not restore or build.</summary>
public interface IProjectEvaluator
{
    /// <param name="targetFramework">Evaluate as this framework (multi-targeted projects can differ per framework); null for the default.</param>
    /// <exception cref="Packages.PackageSourceException">The project could not be evaluated.</exception>
    Task<ProjectEvaluation> EvaluateAsync(string projectPath, string? targetFramework, CancellationToken cancellationToken = default);
}
