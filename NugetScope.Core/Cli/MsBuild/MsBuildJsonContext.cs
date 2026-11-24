using System.Text.Json.Serialization;

namespace NugetScope.Core.Cli.MsBuild;

/// <summary>Source-generated serializer metadata, so parsing works with trimming/AOT (no reflection).</summary>
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(MsBuildEvaluationDocument))]
internal sealed partial class MsBuildJsonContext : JsonSerializerContext;
