namespace NugetScope.Core.Cli.MsBuild;

// DTO for `dotnet msbuild <project> -getItem:... -getProperty:...` (item metadata keyed by name).
internal sealed class MsBuildEvaluationDocument
{
    public Dictionary<string, string>? Properties { get; init; }

    public Dictionary<string, List<Dictionary<string, string>>>? Items { get; init; }
}
