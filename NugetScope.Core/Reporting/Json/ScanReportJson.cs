namespace NugetScope.Core.Reporting.Json;

// DTOs for the `--json` report. The shape is a public contract: add fields, but don't rename or remove them
// without bumping ScanReportDocument.Version.

internal sealed class ScanReportDocument
{
    public int Version { get; init; } = 1;

    public required string Solution { get; init; }

    public required List<string> CompletedChecks { get; init; }

    public required ReportSummaryJson Summary { get; init; }

    public required List<ReportPackageJson> Packages { get; init; }

    public required List<ReportProjectJson> Projects { get; init; }

    public required List<ReportProblemJson> Problems { get; init; }
}

internal sealed class ReportSummaryJson
{
    public int Projects { get; init; }

    public int Packages { get; init; }

    public int Vulnerable { get; init; }

    public int Deprecated { get; init; }

    public int Outdated { get; init; }

    public int Mismatch { get; init; }
}

internal sealed class ReportPackageJson
{
    public required string Id { get; init; }

    public required List<string> Status { get; init; }

    public required List<string> Versions { get; init; }

    public string? Latest { get; init; }

    public bool Transitive { get; init; }

    public ReportDeprecationJson? Deprecation { get; init; }

    public required List<ReportVulnerabilityJson> Vulnerabilities { get; init; }

    public required List<ReportUsageJson> Projects { get; init; }
}

internal sealed class ReportUsageJson
{
    public required string Project { get; init; }

    public required List<string> Frameworks { get; init; }

    public string? Requested { get; init; }

    public string? Resolved { get; init; }

    public string? Latest { get; init; }

    public bool Transitive { get; init; }
}

internal sealed class ReportDeprecationJson
{
    public required List<string> Reasons { get; init; }

    public string? AlternativePackage { get; init; }

    public string? AlternativeVersionRange { get; init; }
}

internal sealed class ReportVulnerabilityJson
{
    public required string Severity { get; init; }

    public string? AdvisoryUrl { get; init; }
}

internal sealed class ReportProjectJson
{
    public required string Name { get; init; }

    public required string Path { get; init; }

    public string? SolutionFolder { get; init; }

    public required List<ReportProblemJson> Problems { get; init; }
}

internal sealed class ReportProblemJson
{
    public required string Level { get; init; }

    public string? Code { get; init; }

    public required string Message { get; init; }
}
