using System.Text;
using NugetScope.Core.Packages;

namespace NugetScope.Tui.Formatting;

/// <summary>Plain-text list of scan problems (restore failures, missing assets, feed errors).</summary>
internal static class ProblemsFormatter
{
    public static int Count(PackageScanResult scan) =>
        scan.Problems.Count + scan.Projects.Sum(p => p.Problems.Count);

    public static string Format(PackageScanResult scan)
    {
        if (Count(scan) == 0)
        {
            return "No problems.";
        }

        var text = new StringBuilder();

        foreach (PackageProblem problem in scan.Problems)
        {
            AppendProblem(text, "Solution", problem);
        }

        foreach (ProjectPackages project in scan.Projects.Where(p => p.Problems.Count > 0))
        {
            foreach (PackageProblem problem in project.Problems)
            {
                AppendProblem(text, project.Project.Name, problem);
            }
        }

        return text.ToString().TrimEnd();
    }

    private static void AppendProblem(StringBuilder text, string scope, PackageProblem problem)
    {
        string code = problem.Code is null ? "" : $" {problem.Code}";
        text.AppendLine($"[{problem.Level}{code}] {scope}: {problem.Message}");
    }
}
