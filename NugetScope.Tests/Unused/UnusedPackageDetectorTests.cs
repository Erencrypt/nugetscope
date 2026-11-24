using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Core.Reporting;
using NugetScope.Core.Solutions;
using NugetScope.Core.Unused;
using NugetScope.Tests.Support;

namespace NugetScope.Tests.Unused;

public sealed class UnusedPackageDetectorTests : IDisposable
{
    // Real assemblies the test project already has: their namespaces are NuGet.Versioning and Microsoft.VisualStudio.*.
    private static readonly string VersioningDll = typeof(NuGet.Versioning.NuGetVersion).Assembly.Location;
    private static readonly string PersistenceDll = typeof(Microsoft.VisualStudio.SolutionPersistence.ISolutionSerializer).Assembly.Location;
    private static readonly string XunitDll = typeof(FactAttribute).Assembly.Location;

    private readonly TempDirectory _repo = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _repo.Dispose();

    [Fact]
    public async Task Flags_only_packages_whose_namespaces_never_appear_and_that_can_be_judged()
    {
        ProjectInfo project = new("App", _repo.CreateFile("App/App.csproj", "<Project />"), null);
        string source = _repo.CreateFile("App/Program.cs", "using NuGet.Versioning;\n\nvar v = NuGetVersion.Parse(\"1.0.0\");");
        _repo.CreateFile("App/Views/MainWindow.xaml", "<Window xmlns:tb=\"using:Xunit\" />"); // Used only from markup.

        string assets = WriteAssetsFile(
            ("Used", VersioningDll, Build: false),
            ("UsedInXaml", XunitDll, Build: false),
            ("Unused", PersistenceDll, Build: false),
            ("DevOnly", PersistenceDll, Build: false),
            ("Hooked", PersistenceDll, Build: true),
            ("Analyzer", null, Build: false));

        var reader = new FakeContentsReader(new ProjectContents(
            assets,
            [source],
            GlobalUsings: [],
            References:
            [
                new DeclaredReference("Used", null, null, null),
                new DeclaredReference("UsedInXaml", null, null, null),
                new DeclaredReference("Unused", null, null, null),
                new DeclaredReference("DevOnly", PrivateAssets: "all", null, null),
                new DeclaredReference("Hooked", null, null, null),
                new DeclaredReference("Analyzer", null, null, null),
            ]));

        var scan = new PackageScanResult(
            [new ProjectPackages(project, [.. new[] { "Used", "UsedInXaml", "Unused", "DevOnly", "Hooked", "Analyzer" }.Select(Reference)], [])],
            []);

        UnusedPackageReport report = await new UnusedPackageDetector(reader).DetectAsync(scan, Ct);

        Assert.True(report.IsPossiblyUnused(project, "unused"));
        Assert.All(["Used", "UsedInXaml", "DevOnly", "Hooked", "Analyzer"], id => Assert.False(report.IsPossiblyUnused(project, id)));
        Assert.Empty(report.Skipped);
    }

    [Fact]
    public void Possibly_unused_is_never_an_issue_or_a_fail_on_value()
    {
        Assert.False(PackageStatusNames.AnyIssue.HasFlag(PackageStatus.PossiblyUnused));
        Assert.False(PackageStatusNames.TryParse("possibly-unused", out _));
        Assert.False(new PackageFilter(Status: PackageStatusFilter.AnyIssue).Matches(
            new PackageSummary("X", [new PackageUsage(new ProjectInfo("A", "C:/A.csproj", null), ["net10.0"], "1.0.0", "1.0.0", false) { IsPossiblyUnused = true }])));
    }

    // Packages live under <repo>/packages/<id>/1.0.0; a null assembly means "nothing to compile against" (_._).
    private string WriteAssetsFile(params (string Id, string? Assembly, bool Build)[] packages)
    {
        string packagesFolder = Path.Combine(_repo.Path, "packages").Replace('\\', '/') + "/";
        var targets = new List<string>();
        var libraries = new List<string>();

        foreach ((string id, string? assembly, bool build) in packages)
        {
            string folder = $"{id.ToLowerInvariant()}/1.0.0";
            string compileFile = assembly is null ? "lib/net10.0/_._" : $"lib/net10.0/{Path.GetFileName(assembly)}";

            if (assembly is not null)
            {
                string target = Path.Combine(_repo.Path, "packages", folder, compileFile);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(assembly, target);
            }

            string buildAssets = build ? $", \"build\": {{ \"build/{id}.targets\": {{}} }}" : "";
            targets.Add($"\"{id}/1.0.0\": {{ \"type\": \"package\", \"compile\": {{ \"{compileFile}\": {{}} }}{buildAssets} }}");
            libraries.Add($"\"{id}/1.0.0\": {{ \"type\": \"package\", \"path\": \"{folder}\" }}");
        }

        return _repo.CreateFile("App/obj/project.assets.json", $$"""
            {
              "version": 3,
              "targets": { "net10.0": { {{string.Join(", ", targets)}} } },
              "libraries": { {{string.Join(", ", libraries)}} },
              "packageFolders": { "{{packagesFolder}}": {} }
            }
            """);
    }

    private static PackageReference Reference(string id) => new()
    {
        Id = id,
        TargetFramework = "net10.0",
        RequestedVersion = "1.0.0",
        ResolvedVersion = "1.0.0",
    };

    private sealed class FakeContentsReader(ProjectContents contents) : IProjectContentsReader
    {
        public Task<ProjectContents> ReadAsync(string projectPath, string? targetFramework, CancellationToken cancellationToken = default) =>
            Task.FromResult(contents);
    }
}
