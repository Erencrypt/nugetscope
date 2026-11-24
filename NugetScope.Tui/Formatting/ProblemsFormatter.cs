using NugetScope.Core.Packages;

namespace NugetScope.Tui.Formatting;

/// <summary>List of scan problems (restore failures, missing assets, feed errors), errors in red and warnings in yellow.</summary>
internal static class ProblemsFormatter
{
    public static int Count(PackageScanResult scan) =>
        scan.Problems.Count + scan.Projects.Sum(p => p.Problems.Count);

    public static IReadOnlyList<DetailLine> Format(PackageScanResult scan)
    {
        if (Count(scan) == 0)
        {
            return [new DetailLine("No problems.")];
        }

        return scan.Problems.Select(problem => Line("Solution", problem))
            .Concat(scan.Projects.SelectMany(project => project.Problems.Select(problem => Line(project.Project.Name, problem))))
            .ToList();
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
