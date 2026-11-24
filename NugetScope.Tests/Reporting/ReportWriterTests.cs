using System.Text.Json;
using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Core.Reporting;
using NugetScope.Core.Scanning;
using NugetScope.Core.Solutions;

namespace NugetScope.Tests.Reporting;

public sealed class ReportWriterTests
{
    private static readonly ProjectInfo A = new("A", Path.GetFullPath("C:/repo/A/A.csproj"), null);

    private static readonly PackageSummary Vulnerable = new("Foo",
    [
        new PackageUsage(A, ["net10.0"], "1.0.0", "1.0.0", IsTransitive: false)
        {
            LatestVersion = "2.0.0",
            Vulnerabilities = [new PackageVulnerability(VulnerabilitySeverity.High, new Uri("https://example.test/advisory"))],
        },
    ]);

    private static readonly SolutionScan Scan = new(
        new SolutionInfo(Path.GetFullPath("C:/repo/Fx.slnx"), [A]),
        new PackageScanResult([new ProjectPackages(A, [], [])], [], PackageChecks.Outdated | PackageChecks.Vulnerable));

    [Fact]
    public void Json_report_has_summary_statuses_and_usages()
    {
        using JsonDocument json = JsonDocument.Parse(JsonReportWriter.Write(Scan, [Vulnerable]));
        JsonElement root = json.RootElement;
        JsonElement package = root.GetProperty("packages")[0];

        Assert.Equal(1, root.GetProperty("version").GetInt32());
        Assert.Equal(["vulnerable", "outdated"], root.GetProperty("completedChecks").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal(1, root.GetProperty("summary").GetProperty("vulnerable").GetInt32());
        Assert.Equal(["vulnerable", "outdated"], package.GetProperty("status").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal("high", package.GetProperty("vulnerabilities")[0].GetProperty("severity").GetString());
        Assert.Equal("A", package.GetProperty("projects")[0].GetProperty("project").GetString());
        Assert.False(package.TryGetProperty("deprecation", out _)); // Nulls are omitted.
    }

    [Fact]
    public void Text_report_marks_unchecked_statuses_and_ends_with_the_gate_result()
    {
        FailureEvaluation evaluation = FailurePolicy.Evaluate(PackageStatus.Vulnerable, [Vulnerable], Scan.Packages);

        string text = TextReportWriter.Write(Scan, [Vulnerable], evaluation);

        Assert.Contains("deprecated: not checked", text);
        Assert.Contains("  Foo 1.0.0 (high) [A]", text);
        Assert.EndsWith("FAILED: 1 package(s) match --fail-on vulnerable." + Environment.NewLine, text);
    }
}
