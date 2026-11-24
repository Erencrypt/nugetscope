using NugetScope.Core.Cli.PackageSearch;
using NugetScope.Core.Packages;
using NugetScope.Core.Updates;

namespace NugetScope.Core.Cli;

/// <summary>Package versions via <c>dotnet package search</c>, which honours the nuget.config of the working directory.</summary>
public sealed class DotnetCliPackageVersionSource(IProcessRunner processRunner) : IPackageVersionSource
{
    public async Task<PackageVersionList> GetVersionsAsync(
        string packageId,
        string workingDirectory,
        CancellationToken cancellationToken = default)
    {
        ProcessResult result = await DotnetCommand.RunAsync(
                processRunner,
                ["package", "search", packageId, "--exact-match", "--prerelease", "--format", "json"],
                workingDirectory,
                cancellationToken)
            .ConfigureAwait(false);

        try
        {
            return PackageSearchParser.Parse(result.StandardOutput, packageId);
        }
        catch (FormatException ex)
        {
            throw new PackageSourceException(
                $"Could not read 'dotnet package search' output (exit code {result.ExitCode}). {result.StandardError}".TrimEnd(),
                ex);
        }
    }
}
