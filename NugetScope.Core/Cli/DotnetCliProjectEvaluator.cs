using NugetScope.Core.Cli.MsBuild;
using NugetScope.Core.Packages;
using NugetScope.Core.Updates;

namespace NugetScope.Core.Cli;

/// <summary>Evaluates a project with <c>dotnet msbuild -getItem</c>: fast, no restore, and it follows imports such as <c>Directory.Packages.props</c>.</summary>
public sealed class DotnetCliProjectEvaluator(IProcessRunner processRunner) : IProjectEvaluator
{
    public async Task<ProjectEvaluation> EvaluateAsync(
        string projectPath,
        string? targetFramework,
        CancellationToken cancellationToken = default)
    {
        List<string> arguments =
        [
            "msbuild",
            projectPath,
            "-getItem:PackageReference",
            "-getItem:PackageVersion",
            $"-getProperty:{MsBuildEvaluationParser.CentralPackageManagementProperty}",
        ];

        if (targetFramework is not null)
        {
            arguments.Add($"-p:TargetFramework={targetFramework}");
        }

        ProcessResult result = await DotnetCommand.RunAsync(
                processRunner, arguments, Path.GetDirectoryName(projectPath)!, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            if (result.ExitCode == 0)
            {
                return MsBuildEvaluationParser.Parse(result.StandardOutput);
            }
        }
        catch (FormatException)
        {
            // Reported below with the CLI's own output.
        }

        // msbuild prints evaluation errors (e.g. "error MSB4025") to stdout.
        string error = (result.StandardOutput + "\n" + result.StandardError)
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(line => line.Contains("error", StringComparison.OrdinalIgnoreCase))
            ?? $"exit code {result.ExitCode}";

        throw new PackageSourceException($"Could not evaluate the project: {error}");
    }
}
