namespace NugetScope.Core.Unused;

/// <param name="AssetsFile">The restore output (<c>project.assets.json</c>) listing each package's assemblies.</param>
/// <param name="SourceFiles">Full paths of the C# files the project compiles.</param>
/// <param name="GlobalUsings">Namespaces imported for every file (<c>&lt;Using&gt;</c> items, including implicit usings).</param>
public sealed record ProjectContents(
    string? AssetsFile,
    IReadOnlyList<string> SourceFiles,
    IReadOnlyList<string> GlobalUsings,
    IReadOnlyList<DeclaredReference> References);
