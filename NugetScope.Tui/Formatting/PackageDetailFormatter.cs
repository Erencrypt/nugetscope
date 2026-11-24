using System.Text;
using NugetScope.Core.Analysis;

namespace NugetScope.Tui.Formatting;

/// <summary>Plain-text details for one package, for the detail pane.</summary>
internal static class PackageDetailFormatter
{
    public static string Format(PackageSummary package)
    {
        var text = new StringBuilder();

        text.AppendLine(package.Id);
        text.AppendLine(new string('─', package.Id.Length));
        text.Append("Versions in use: ").AppendLine(string.Join(", ", package.ResolvedVersions));

        if (package.HasVersionMismatch)
        {
            text.AppendLine("Version mismatch: projects resolve different versions.");
        }

        text.AppendLine();
        text.AppendLine($"Projects ({package.ProjectCount}):");

        int nameWidth = package.Usages.Max(u => u.Project.Name.Length);
        foreach (PackageUsage usage in package.Usages)
        {
            text.Append("  ")
                .Append(usage.Project.Name.PadRight(nameWidth))
                .Append("  ")
                .Append(FormatVersion(usage))
                .Append("  ")
                .AppendLine(string.Join(", ", usage.TargetFrameworks));
        }

        return text.ToString();
    }

    // Show the requested version only when it differs from what was resolved (floating versions, ranges).
    private static string FormatVersion(PackageUsage usage)
    {
        string resolved = usage.ResolvedVersion ?? "?";

        if (usage.IsTransitive)
        {
            return $"{resolved} (transitive)";
        }

        return usage.RequestedVersion is { } requested && !requested.Equals(resolved, StringComparison.OrdinalIgnoreCase)
            ? $"{requested} -> {resolved}"
            : resolved;
    }
}
