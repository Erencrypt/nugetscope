namespace NugetScope.Core.Cli.PackageSearch;

// DTOs for `dotnet package search <id> --exact-match --format json` (schema version 2).

internal sealed class PackageSearchDocument
{
    public List<SearchProblemJson>? Problems { get; init; }

    public List<SearchSourceJson>? SearchResult { get; init; }
}

internal sealed class SearchSourceJson
{
    public string? SourceName { get; init; }

    public List<SearchProblemJson>? Problems { get; init; }

    public List<SearchPackageJson>? Packages { get; init; }
}

internal sealed class SearchPackageJson
{
    public string? Id { get; init; }

    public string? Version { get; init; }
}

internal sealed class SearchProblemJson
{
    public string? Text { get; init; }

    public string? ProblemType { get; init; }
}
