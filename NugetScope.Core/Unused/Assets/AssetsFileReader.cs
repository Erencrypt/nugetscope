using System.Text.Json;

namespace NugetScope.Core.Unused.Assets;

/// <summary>Reads <c>project.assets.json</c> into the compile assemblies and build hooks of each package.</summary>
internal static class AssetsFileReader
{
    // NuGet writes "_._" for "nothing for this framework, on purpose".
    private const string EmptyFolderMarker = "_._";

    /// <exception cref="FormatException">The file is not a readable assets file.</exception>
    /// <exception cref="IOException">The file could not be read.</exception>
    public static IReadOnlyDictionary<string, PackageAssets> Read(string path)
    {
        AssetsFileDocument document;
        try
        {
            using FileStream stream = File.OpenRead(path);
            document = JsonSerializer.Deserialize(stream, AssetsFileJsonContext.Default.AssetsFileDocument)
                ?? throw new FormatException("The assets file is empty.");
        }
        catch (JsonException ex)
        {
            throw new FormatException("The assets file is not valid JSON.", ex);
        }

        var folders = (document.PackageFolders ?? []).Keys.ToList();
        var libraries = document.Libraries ?? [];

        return (document.Targets ?? [])
            .SelectMany(target => target.Value)
            .Where(entry => string.Equals(entry.Value.Type, "package", StringComparison.OrdinalIgnoreCase))
            .GroupBy(entry => PackageId(entry.Key), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => new PackageAssets(
                    group
                        .SelectMany(entry => (entry.Value.Compile ?? []).Keys
                            .Where(file => !file.EndsWith(EmptyFolderMarker, StringComparison.Ordinal))
                            .Select(file => Locate(folders, libraries.GetValueOrDefault(entry.Key)?.Path, file)))
                        .OfType<string>()
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList(),
                    group.Any(entry => HasFiles(entry.Value.Build) || HasFiles(entry.Value.BuildMultiTargeting))),
                StringComparer.OrdinalIgnoreCase);
    }

    private static string PackageId(string idAndVersion)
    {
        int slash = idAndVersion.LastIndexOf('/');
        return slash < 0 ? idAndVersion : idAndVersion[..slash];
    }

    private static bool HasFiles(Dictionary<string, JsonElement>? files) =>
        files?.Keys.Any(file => !file.EndsWith(EmptyFolderMarker, StringComparison.Ordinal)) == true;

    // A package can live in any of the package folders (the user folder, then fallback folders).
    private static string? Locate(IReadOnlyList<string> folders, string? libraryPath, string file) =>
        libraryPath is null
            ? null
            : folders
                .Select(folder => Path.GetFullPath(Path.Combine(folder, libraryPath, file)))
                .FirstOrDefault(File.Exists);
}
