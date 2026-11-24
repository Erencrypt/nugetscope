using System.Text.Json;

namespace NugetScope.Core.Unused.Assets;

// The parts of NuGet's project.assets.json that say which files each restored package contributes.

internal sealed class AssetsFileDocument
{
    /// <summary>Per target framework: "Id/Version" → that package's assets.</summary>
    public Dictionary<string, Dictionary<string, AssetsTargetJson>>? Targets { get; init; }

    /// <summary>"Id/Version" → where the package lives under a package folder.</summary>
    public Dictionary<string, AssetsLibraryJson>? Libraries { get; init; }

    public Dictionary<string, JsonElement>? PackageFolders { get; init; }
}

internal sealed class AssetsTargetJson
{
    public string? Type { get; init; }

    public Dictionary<string, JsonElement>? Compile { get; init; }

    public Dictionary<string, JsonElement>? Build { get; init; }

    public Dictionary<string, JsonElement>? BuildMultiTargeting { get; init; }
}

internal sealed class AssetsLibraryJson
{
    public string? Type { get; init; }

    public string? Path { get; init; }
}
