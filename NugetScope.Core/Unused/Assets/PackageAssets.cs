namespace NugetScope.Core.Unused.Assets;

/// <summary>What a restored package contributes to a project.</summary>
/// <param name="CompileAssemblies">Full paths of the assemblies code compiles against (all target frameworks).</param>
/// <param name="HooksIntoBuild">Ships MSBuild props/targets, so it may work without any code referencing it.</param>
internal sealed record PackageAssets(IReadOnlyList<string> CompileAssemblies, bool HooksIntoBuild);
