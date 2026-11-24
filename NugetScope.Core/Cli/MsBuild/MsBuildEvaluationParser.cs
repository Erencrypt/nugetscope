using System.Text.Json;
using NugetScope.Core.Updates;

namespace NugetScope.Core.Cli.MsBuild;

/// <summary>Parses the JSON printed by <c>dotnet msbuild -getItem:PackageReference -getItem:PackageVersion -getProperty:ManagePackageVersionsCentrally</c>.</summary>
internal static class MsBuildEvaluationParser
{
    public const string CentralPackageManagementProperty = "ManagePackageVersionsCentrally";

    /// <exception cref="FormatException">The output is not msbuild evaluation JSON.</exception>
    public static ProjectEvaluation Parse(string output)
    {
        string json = DotnetCommand.JsonBody(output) ?? throw new FormatException("msbuild produced no JSON output.");

        MsBuildEvaluationDocument document;
        try
        {
            document = JsonSerializer.Deserialize(json, MsBuildJsonContext.Default.MsBuildEvaluationDocument)
                ?? throw new FormatException("msbuild produced empty JSON.");
        }
        catch (JsonException ex)
        {
            throw new FormatException("msbuild produced invalid JSON.", ex);
        }

        string? central = document.Properties?.GetValueOrDefault(CentralPackageManagementProperty);

        return new ProjectEvaluation(
            string.Equals(central, "true", StringComparison.OrdinalIgnoreCase),
            Items(document, "PackageReference"),
            Items(document, "PackageVersion"));
    }

    private static List<EvaluatedPackageItem> Items(MsBuildEvaluationDocument document, string itemType) =>
        (document.Items?.GetValueOrDefault(itemType) ?? [])
            .Select(metadata => new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase))
            .Where(metadata => !string.IsNullOrEmpty(metadata.GetValueOrDefault("Identity"))
                && !string.IsNullOrEmpty(metadata.GetValueOrDefault("DefiningProjectFullPath")))
            .Select(metadata => new EvaluatedPackageItem(
                metadata["Identity"],
                NullIfEmpty(metadata.GetValueOrDefault("Version")),
                NullIfEmpty(metadata.GetValueOrDefault("VersionOverride")),
                metadata["DefiningProjectFullPath"]))
            .ToList();

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;
}
