using NugetScope.Core.IO;
using NugetScope.Core.Packages;
using NugetScope.Core.Updates;

namespace NugetScope.Tests.Support;

/// <summary>Returns a fixed evaluation per project path; unknown projects fail like an unreadable project would.</summary>
internal sealed class FakeProjectEvaluator : IProjectEvaluator
{
    private readonly Dictionary<string, ProjectEvaluation> _evaluations = new(PathNormalizer.Comparer);

    public FakeProjectEvaluator On(string projectPath, ProjectEvaluation evaluation)
    {
        _evaluations[PathNormalizer.Normalize(projectPath)] = evaluation;
        return this;
    }

    public Task<ProjectEvaluation> EvaluateAsync(string projectPath, string? targetFramework, CancellationToken cancellationToken = default) =>
        _evaluations.TryGetValue(PathNormalizer.Normalize(projectPath), out ProjectEvaluation? evaluation)
            ? Task.FromResult(evaluation)
            : throw new PackageSourceException($"Could not evaluate {projectPath}.");
}
