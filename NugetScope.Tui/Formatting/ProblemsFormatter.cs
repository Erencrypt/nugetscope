using NugetScope.Core.Packages;
using NugetScope.Core.Solutions;

namespace NugetScope.Tui.Formatting;

/// <summary>List of scan problems (restore failures, missing assets, feed errors), errors in red and warnings in yellow.</summary>
internal static class ProblemsFormatter
{
    public static int Count(PackageScanResult scan) =>
        scan.Problems.Count + scan.Projects.Sum(p => p.Problems.Count);

    /// <param name="unusedCheckSkipped">Projects the possibly-unused check couldn't run for, listed as info.</param>
    public static IReadOnlyList<DetailLine> Format(
        PackageScanResult scan,
        IReadOnlyDictionary<ProjectInfo, string>? unusedCheckSkipped = null)
    {
        var lines = scan.Problems.Select(problem => Line("Solution", problem))
            .Concat(scan.Projects.SelectMany(project => project.Problems.Select(problem => Line(project.Project.Name, problem))))
            .Concat((unusedCheckSkipped ?? new Dictionary<ProjectInfo, string>()).Select(skip => Line(
                skip.Key.Name,
                new PackageProblem(ProblemLevel.Info, $"possibly-unused check skipped: {skip.Value}"))))
            .ToList();

        return lines.Count > 0 ? lines : [new DetailLine("No problems.")];
    }

    private static DetailLine Line(string scope, PackageProblem problem)
    {
        string code = problem.Code is null ? "" : $" {problem.Code}";
        DetailTone tone = problem.Level switch
        {
            ProblemLevel.Error => DetailTone.Error,
            ProblemLevel.Warning => DetailTone.Warning,
            _ => DetailTone.Normal,
        };

        return new DetailLine($"[{problem.Level}{code}] {scope}: {problem.Message}", tone);
    }
}
