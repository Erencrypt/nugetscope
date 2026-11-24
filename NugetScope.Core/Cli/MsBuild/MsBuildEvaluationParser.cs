using System.Text.Json;
using NugetScope.Core.Updates;

namespace NugetScope.Core.Cli.MsBuild;

/// <summary>Parses the JSON printed by <c>dotnet msbuild -getItem:... -getProperty:...</c>.</summary>
internal static class MsBuildEvaluationParser
{
    public const string CentralPackageManagementProperty = "ManagePackageVersionsCentrally";

    /// <summary>Package items for the update planner (<c>-getItem:PackageReference -getItem:PackageVersion -getProperty:ManagePackageVersionsCentrally</c>).</summary>
    /// <exception cref="FormatException">The output is not msbuild evaluation JSON.</exception>
    public static ProjectEvaluation Parse(string output)
    {
        MsBuildEvaluationDocument document = ParseDocument(output);

        return new ProjectEvaluation(
            string.Equals(Property(document, CentralPackageManagementProperty), "true", StringComparison.OrdinalIgnoreCase),
            ToPackageItems(Items(document, "PackageReference")),
            ToPackageItems(Items(document, "PackageVersion")));
    }

    /// <exception cref="FormatException">The output is not msbuild evaluation JSON.</exception>
    public static MsBuildEvaluationDocument ParseDocument(string output)
    {
        string json = DotnetCommand.JsonBody(output) ?? throw new FormatException("msbuild produced no JSON output.");

        try
        {
            return JsonSerializer.Deserialize(json, MsBuildJsonContext.Default.MsBuildEvaluationDocument)
                ?? throw new FormatException("msbuild produced empty JSON.");
        }
        catch (JsonException ex)
        {
            throw new FormatException("msbuild produced invalid JSON.", ex);
        }
    }

    public static string? Property(MsBuildEvaluationDocument document, string name) =>
        NullIfEmpty(document.Properties?.GetValueOrDefault(name));

    /// <summary>Items of one type, with metadata names matched case-insensitively (as MSBuild does).</summary>
    public static List<Dictionary<string, string>> Items(MsBuildEvaluationDocument document, string itemType) =>
        (document.Items?.GetValueOrDefault(itemType) ?? [])
            .Select(metadata => new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase))
            .Where(metadata => !string.IsNullOrEmpty(metadata.GetValueOrDefault("Identity")))
            .ToList();

    public static string? Metadata(Dictionary<string, string> item, string name) => NullIfEmpty(item.GetValueOrDefault(name));

    private static List<EvaluatedPackageItem> ToPackageItems(List<Dictionary<string, string>> items) =>
        items
            .Where(item => Metadata(item, "DefiningProjectFullPath") is not null)
            .Select(item => new EvaluatedPackageItem(
                item["Identity"],
                Metadata(item, "Version"),
                Metadata(item, "VersionOverride"),
                item["DefiningProjectFullPath"]))
            .ToList();

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;
}
