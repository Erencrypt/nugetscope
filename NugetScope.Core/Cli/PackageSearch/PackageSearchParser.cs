using System.Text.Json;
using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Core.Updates;

namespace NugetScope.Core.Cli.PackageSearch;

/// <summary>
/// Parses <c>dotnet package search --exact-match --format json</c>. The command exits 0 even when a source fails,
/// so per-source problems are reported instead.
/// </summary>
internal static class PackageSearchParser
{
    /// <exception cref="FormatException">The output is not package search JSON.</exception>
    public static PackageVersionList Parse(string output, string packageId)
    {
        string json = DotnetCommand.JsonBody(output) ?? throw new FormatException("dotnet package search produced no JSON output.");

        PackageSearchDocument document;
        try
        {
            document = JsonSerializer.Deserialize(json, PackageSearchJsonContext.Default.PackageSearchDocument)
                ?? throw new FormatException("dotnet package search produced empty JSON.");
        }
        catch (JsonException ex)
        {
            throw new FormatException("dotnet package search produced invalid JSON.", ex);
        }

        var sources = document.SearchResult ?? [];

        var versions = sources
            .SelectMany(s => s.Packages ?? [])
            .Where(p => string.Equals(p.Id, packageId, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(p.Version))
            .Select(p => p.Version!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderDescending(VersionStringComparer.Instance)
            .ToList();

        var problems = (document.Problems ?? []).Select(p => ToProblem(p, source: null))
            .Concat(sources.SelectMany(s => (s.Problems ?? []).Select(p => ToProblem(p, s.SourceName))))
            .ToList();

        return new PackageVersionList(versions, problems);
    }

    private static PackageProblem ToProblem(SearchProblemJson problem, string? source)
    {
        var level = string.Equals(problem.ProblemType, "Warning", StringComparison.OrdinalIgnoreCase)
            ? ProblemLevel.Warning
            : ProblemLevel.Error;
        string text = problem.Text ?? "Unknown problem.";

        return new PackageProblem(level, source is null ? text : $"{source}: {text}");
    }
}
