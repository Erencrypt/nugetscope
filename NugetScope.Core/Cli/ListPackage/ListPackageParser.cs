using System.Text.Json;
using NugetScope.Core.IO;
using NugetScope.Core.Packages;

namespace NugetScope.Core.Cli.ListPackage;

/// <summary>Parses <c>dotnet list package --format json --output-version 1</c>.</summary>
internal static class ListPackageParser
{
    public const int SupportedOutputVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <exception cref="FormatException">The output is not valid list-package JSON.</exception>
    public static ListPackageReport Parse(string output)
    {
        ListPackageJson json = Deserialize(output);

        if (json.Version != SupportedOutputVersion)
        {
            throw new FormatException($"Unsupported dotnet list package output version {json.Version}.");
        }

        var projects = new Dictionary<string, ProjectBuilder>(PathNormalizer.Comparer);
        var solutionProblems = new List<PackageProblem>();

        foreach (ProjectJson project in json.Projects ?? [])
        {
            GetOrAdd(projects, project.Path).Packages.AddRange(ToPackages(project));
        }

        foreach (ProblemJson problem in json.Problems ?? [])
        {
            var mapped = new PackageProblem(ToLevel(problem.Level), problem.Text ?? "Unknown problem.");

            if (string.IsNullOrWhiteSpace(problem.Project))
            {
                solutionProblems.Add(mapped);
            }
            else
            {
                GetOrAdd(projects, problem.Project).Problems.Add(mapped);
            }
        }

        return new ListPackageReport(
            projects.ToDictionary(p => p.Key, p => p.Value.Build(), PathNormalizer.Comparer),
            solutionProblems);
    }

    private static ListPackageJson Deserialize(string output)
    {
        // Tolerate stray lines (e.g. SDK notices) before the JSON body.
        int start = output.IndexOf('{');
        if (start < 0)
        {
            throw new FormatException("dotnet list package produced no JSON output.");
        }

        try
        {
            return JsonSerializer.Deserialize<ListPackageJson>(output.AsSpan(start), JsonOptions)
                ?? throw new FormatException("dotnet list package produced empty JSON.");
        }
        catch (JsonException ex)
        {
            throw new FormatException("dotnet list package produced invalid JSON.", ex);
        }
    }

    private static IEnumerable<PackageReference> ToPackages(ProjectJson project)
    {
        foreach (FrameworkJson framework in project.Frameworks ?? [])
        {
            foreach (PackageJson package in framework.TopLevelPackages ?? [])
            {
                yield return ToPackage(package, framework.Framework, isTransitive: false);
            }

            foreach (PackageJson package in framework.TransitivePackages ?? [])
            {
                yield return ToPackage(package, framework.Framework, isTransitive: true);
            }
        }
    }

    private static PackageReference ToPackage(PackageJson package, string framework, bool isTransitive) => new()
    {
        Id = package.Id,
        TargetFramework = framework,
        RequestedVersion = package.RequestedVersion,
        ResolvedVersion = package.ResolvedVersion,
        IsTransitive = isTransitive,
        IsAutoReferenced = string.Equals(package.AutoReferenced, "true", StringComparison.OrdinalIgnoreCase),
        LatestVersion = package.LatestVersion,
        Deprecation = ToDeprecation(package),
        Vulnerabilities = package.Vulnerabilities?.Select(ToVulnerability).ToList() ?? [],
    };

    private static PackageDeprecation? ToDeprecation(PackageJson package)
    {
        if (package.DeprecationReasons is not { Count: > 0 } && package.AlternativePackage is null)
        {
            return null;
        }

        return new PackageDeprecation(
            package.DeprecationReasons ?? [],
            package.AlternativePackage?.Id,
            package.AlternativePackage?.VersionRange);
    }

    private static PackageVulnerability ToVulnerability(VulnerabilityJson vulnerability)
    {
        var severity = Enum.TryParse(vulnerability.Severity, ignoreCase: true, out VulnerabilitySeverity parsed)
            ? parsed
            : VulnerabilitySeverity.Unknown;

        Uri? url = Uri.TryCreate(vulnerability.AdvisoryUrl, UriKind.Absolute, out Uri? parsedUrl) ? parsedUrl : null;

        return new PackageVulnerability(severity, url);
    }

    private static ProblemLevel ToLevel(string? level) => level?.ToLowerInvariant() switch
    {
        "info" => ProblemLevel.Info,
        "warning" => ProblemLevel.Warning,
        _ => ProblemLevel.Error, // Unknown levels are surfaced as errors rather than hidden.
    };

    private static ProjectBuilder GetOrAdd(Dictionary<string, ProjectBuilder> projects, string rawPath)
    {
        string path = PathNormalizer.Normalize(rawPath);
        if (!projects.TryGetValue(path, out ProjectBuilder? builder))
        {
            builder = new ProjectBuilder(path);
            projects[path] = builder;
        }

        return builder;
    }

    private sealed class ProjectBuilder(string path)
    {
        public List<PackageReference> Packages { get; } = [];

        public List<PackageProblem> Problems { get; } = [];

        public ListedProject Build() => new(path, Packages, Problems);
    }
}
