namespace NugetScope.Core.Unused;

/// <summary>Reads what a project compiles and references, without building it.</summary>
public interface IProjectContentsReader
{
    /// <param name="targetFramework">Evaluate as this framework (for multi-targeted projects); null for the default.</param>
    /// <exception cref="Packages.PackageSourceException">The project could not be evaluated.</exception>
    Task<ProjectContents> ReadAsync(string projectPath, string? targetFramework, CancellationToken cancellationToken = default);
}
