using NugetScope.Core.Cli.MsBuild;
using NugetScope.Core.Packages;
using NugetScope.Core.Unused;

namespace NugetScope.Core.Cli;

/// <summary>Project contents via <c>dotnet msbuild -getItem:Compile -getItem:Using -getItem:Import -getItem:PackageReference -getProperty:ProjectAssetsFile</c>.</summary>
public sealed class DotnetCliProjectContentsReader(IProcessRunner processRunner) : IProjectContentsReader
{
    public async Task<ProjectContents> ReadAsync(
        string projectPath,
        string? targetFramework,
        CancellationToken cancellationToken = default)
    {
        List<string> arguments =
        [
            "msbuild",
            projectPath,
            "-getProperty:ProjectAssetsFile",
            "-getItem:Compile",
            "-getItem:Using",
            "-getItem:Import", // Visual Basic's project-level Imports.
            "-getItem:PackageReference",
        ];

        if (targetFramework is not null)
        {
            arguments.Add($"-p:TargetFramework={targetFramework}");
        }

        ProcessResult result = await DotnetCommand.RunAsync(
                processRunner, arguments, Path.GetDirectoryName(projectPath)!, cancellationToken)
            .ConfigureAwait(false);

        MsBuildEvaluationDocument document;
        try
        {
            document = result.ExitCode == 0
                ? MsBuildEvaluationParser.ParseDocument(result.StandardOutput)
                : throw new FormatException($"exit code {result.ExitCode}");
        }
        catch (FormatException ex)
        {
            throw new PackageSourceException($"Could not evaluate the project ({ex.Message}).", ex);
        }

        return new ProjectContents(
            MsBuildEvaluationParser.Property(document, "ProjectAssetsFile"),
            MsBuildEvaluationParser.Items(document, "Compile")
                .Select(item => MsBuildEvaluationParser.Metadata(item, "FullPath"))
                .OfType<string>()
                .ToList(),
            MsBuildEvaluationParser.Items(document, "Using")
                .Concat(MsBuildEvaluationParser.Items(document, "Import"))
                .Select(item => item["Identity"])
                .ToList(),
            MsBuildEvaluationParser.Items(document, "PackageReference")
                .Select(item => new DeclaredReference(
                    item["Identity"],
                    MsBuildEvaluationParser.Metadata(item, "PrivateAssets"),
                    MsBuildEvaluationParser.Metadata(item, "IncludeAssets"),
                    MsBuildEvaluationParser.Metadata(item, "ExcludeAssets")))
                .ToList());
    }
}
