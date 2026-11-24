using System.Text.Json.Serialization;

namespace NugetScope.Core.Unused.Assets;

/// <summary>Source-generated serializer metadata, so parsing works with trimming/AOT (no reflection).</summary>
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(AssetsFileDocument))]
internal sealed partial class AssetsFileJsonContext : JsonSerializerContext;
