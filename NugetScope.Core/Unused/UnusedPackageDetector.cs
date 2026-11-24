using NugetScope.Core.Packages;
using NugetScope.Core.Solutions;
using NugetScope.Core.Unused.Assets;

namespace NugetScope.Core.Unused;

/// <summary>
/// Best-effort "possibly unused" check: a direct package reference is flagged when none of the namespaces its
/// compile assemblies define appear in the project's sources, markup or global usings. Anything it can't judge
/// (no compile assemblies, build-integrated or development-only packages, unreadable files) counts as used.
/// Reflection, DI scanning or configuration-driven loading can't be seen, so results are hints, never facts.
/// </summary>
public sealed class UnusedPackageDetector(IProjectContentsReader reader)
{
    private const int MaxParallelProjects = 4;

    // Files outside Compile items that can name namespaces or types:
    // Razor/MVC views (@using), XAML (xmlns:x="using:A.B" or "clr-namespace:A.B;assembly=..."), Web Forms
    // (<%@ Import Namespace="A.B" %>), web/app.config (type="A.B.C, A.B") and appsettings (e.g. Serilog's "Using").
    private static readonly string[] MarkupPatterns =
    [
        "*.razor", "*.cshtml", "*.vbhtml",
        "*.xaml", "*.axaml",
        "*.aspx", "*.ascx", "*.master", "*.asax", "*.ashx", "*.asmx",
        "*.config", "appsettings*.json",
    ];

    // These list package ids, which often equal namespaces: reading them would make every package look used.
    private static readonly HashSet<string> SkippedFiles = new(StringComparer.OrdinalIgnoreCase) { "nuget.config", "packages.config" };
    private static readonly HashSet<string> SkippedFolders = new(StringComparer.OrdinalIgnoreCase) { "bin", "obj", "node_modules", ".git" };

    private readonly AssemblyNamespaceReader _namespaces = new();

    public async Task<UnusedPackageReport> DetectAsync(PackageScanResult scan, CancellationToken cancellationToken = default)
    {
        using var throttle = new SemaphoreSlim(MaxParallelProjects);
        ProjectResult[] results = await Task.WhenAll(scan.Projects.Select(p => DetectAsync(p, throttle, cancellationToken)))
            .ConfigureAwait(false);

        return new UnusedPackageReport(
            results.Where(r => r.Unused.Count > 0).ToDictionary(r => r.Project, r => (IReadOnlySet<string>)r.Unused),
            results.Where(r => r.SkipReason is not null).ToDictionary(r => r.Project, r => r.SkipReason!));
    }

    private async Task<ProjectResult> DetectAsync(ProjectPackages project, SemaphoreSlim throttle, CancellationToken cancellationToken)
    {
        var direct = project.Packages
            .Where(p => !p.IsTransitive && !p.IsAutoReferenced)
            .Select(p => p.Id)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (direct.Count == 0)
        {
            return new ProjectResult(project.Project, [], null);
        }

        if (project.HasErrors)
        {
            return Skip(project.Project, "the project failed to restore.");
        }

        await throttle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ProjectContents contents = await reader
                .ReadAsync(project.Project.FilePath, project.Packages.Select(p => p.TargetFramework).FirstOrDefault(), cancellationToken)
                .ConfigureAwait(false);

            // Reading assemblies and sources is blocking work; keep it off the caller's thread.
            return await Task.Run(() => Analyze(project.Project, direct, contents), cancellationToken).ConfigureAwait(false);
        }
        catch (PackageSourceException ex)
        {
            return Skip(project.Project, ex.Message);
        }
        catch (Exception ex) when (ex is FormatException or IOException or UnauthorizedAccessException)
        {
            return Skip(project.Project, $"its restore output could not be read ({ex.Message}).");
        }
        finally
        {
            throttle.Release();
        }
    }

    private ProjectResult Analyze(ProjectInfo project, IReadOnlyList<string> direct, ProjectContents contents)
    {
        if (contents.AssetsFile is null || !File.Exists(contents.AssetsFile))
        {
            return Skip(project, "no restore output (project.assets.json) was found.");
        }

        IReadOnlyDictionary<string, PackageAssets> assets = AssetsFileReader.Read(contents.AssetsFile);
        HashSet<string> identifiers = CollectIdentifiers(project, contents);
        var references = contents.References
            .GroupBy(r => r.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last(), StringComparer.OrdinalIgnoreCase);

        var unused = direct
            .Where(id => IsPossiblyUnused(references.GetValueOrDefault(id), assets.GetValueOrDefault(id), identifiers))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new ProjectResult(project, unused, null);
    }

    private bool IsPossiblyUnused(DeclaredReference? reference, PackageAssets? assets, HashSet<string> identifiers)
    {
        if (reference is not { ProvidesCompileAssets: true } || assets is null || assets.HooksIntoBuild || assets.CompileAssemblies.Count == 0)
        {
            return false;
        }

        var namespaces = assets.CompileAssemblies.SelectMany(_namespaces.Read).ToList();

        return namespaces.Count > 0 && !namespaces.Any(identifiers.Contains);
    }

    private static HashSet<string> CollectIdentifiers(ProjectInfo project, ProjectContents contents)
    {
        var identifiers = new HashSet<string>(StringComparer.Ordinal);

        foreach (string globalUsing in contents.GlobalUsings)
        {
            SourceIdentifiers.AddFrom(globalUsing, identifiers);
        }

        string projectDirectory = Path.GetDirectoryName(project.FilePath)!;
        string intermediateDirectory = Path.GetDirectoryName(contents.AssetsFile!)!;

        // The generated global usings file is a fallback for usings the evaluation didn't list.
        IEnumerable<string> generatedUsings = Directory.Exists(intermediateDirectory)
            ? Directory.EnumerateFiles(intermediateDirectory, "*.GlobalUsings.g.cs", SearchOption.AllDirectories)
            : [];

        foreach (string file in contents.SourceFiles.Concat(MarkupFiles(projectDirectory)).Concat(generatedUsings))
        {
            if (File.Exists(file))
            {
                SourceIdentifiers.AddFrom(File.ReadAllText(file), identifiers);
            }
        }

        return identifiers;
    }

    // Markup and config files can name namespaces too (see MarkupPatterns); skip output and dependency folders (node_modules can be huge).
    private static IEnumerable<string> MarkupFiles(string directory)
    {
        var pending = new Stack<string>([directory]);
        var options = new EnumerationOptions { IgnoreInaccessible = true };

        while (pending.TryPop(out string? current))
        {
            foreach (string pattern in MarkupPatterns)
            {
                foreach (string file in Directory.EnumerateFiles(current, pattern, options))
                {
                    if (!SkippedFiles.Contains(Path.GetFileName(file)))
                    {
                        yield return file;
                    }
                }
            }

            foreach (string child in Directory.EnumerateDirectories(current, "*", options))
            {
                if (!SkippedFolders.Contains(Path.GetFileName(child)))
                {
                    pending.Push(child);
                }
            }
        }
    }

    private static ProjectResult Skip(ProjectInfo project, string reason) => new(project, [], reason);

    private sealed record ProjectResult(ProjectInfo Project, HashSet<string> Unused, string? SkipReason);
}
