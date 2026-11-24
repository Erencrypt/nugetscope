using System.Globalization;
using NugetScope.Core.Packages;

namespace NugetScope.Core.Cli.ListPackage;

internal static class ListPackageArguments
{
    /// <summary>Status queries for the requested checks, in a stable order.</summary>
    public static IReadOnlyList<(ListPackageQuery Query, PackageChecks Check)> StatusQueries(PackageChecks checks)
    {
        var queries = new List<(ListPackageQuery, PackageChecks)>();

        if (checks.HasFlag(PackageChecks.Outdated))
        {
            queries.Add((ListPackageQuery.Outdated, PackageChecks.Outdated));
        }

        if (checks.HasFlag(PackageChecks.Deprecated))
        {
            queries.Add((ListPackageQuery.Deprecated, PackageChecks.Deprecated));
        }

        if (checks.HasFlag(PackageChecks.Vulnerable))
        {
            queries.Add((ListPackageQuery.Vulnerable, PackageChecks.Vulnerable));
        }

        return queries;
    }

    /// <summary>Always <c>--no-restore</c>: restore runs once up front, and this keeps one broken project from blanking the output.</summary>
    public static IReadOnlyList<string> Build(string solutionPath, ListPackageQuery query, bool includeTransitive)
    {
        var arguments = new List<string>
        {
            "list", solutionPath, "package",
            "--format", "json",
            "--output-version", ListPackageParser.SupportedOutputVersion.ToString(CultureInfo.InvariantCulture),
            "--no-restore",
        };

        string? statusFlag = query switch
        {
            ListPackageQuery.Outdated => "--outdated",
            ListPackageQuery.Deprecated => "--deprecated",
            ListPackageQuery.Vulnerable => "--vulnerable",
            _ => null,
        };

        if (statusFlag is not null)
        {
            arguments.Add(statusFlag);
        }

        if (includeTransitive)
        {
            arguments.Add("--include-transitive");
        }

        return arguments;
    }
}
