using System.Text.Json.Serialization;

namespace NugetScope.Core.Cli.ListPackage;

/// <summary>Source-generated serializer metadata, so parsing works with trimming/AOT (no reflection).</summary>
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(ListPackageDocument))]
internal sealed partial class ListPackageJsonContext : JsonSerializerContext;
