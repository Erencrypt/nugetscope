using System.Text.Json.Serialization;

namespace NugetScope.Core.Reporting.Json;

/// <summary>Source-generated serializer metadata, so writing works with trimming/AOT (no reflection).</summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ScanReportDocument))]
internal sealed partial class ScanReportJsonContext : JsonSerializerContext;
