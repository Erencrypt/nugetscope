using System.Text.Json.Serialization;

namespace NugetScope.Core.Cli.PackageSearch;

/// <summary>Source-generated serializer metadata, so parsing works with trimming/AOT (no reflection).</summary>
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(PackageSearchDocument))]
internal sealed partial class PackageSearchJsonContext : JsonSerializerContext;
