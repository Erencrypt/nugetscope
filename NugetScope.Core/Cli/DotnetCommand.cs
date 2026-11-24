using System.ComponentModel;
using NugetScope.Core.Packages;

namespace NugetScope.Core.Cli;

/// <summary>Runs <c>dotnet</c> commands, turning a missing SDK into a readable <see cref="PackageSourceException"/>.</summary>
internal static class DotnetCommand
{
    private const string DotnetExecutable = "dotnet";

    public static async Task<ProcessResult> RunAsync(
        IProcessRunner processRunner,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        try
        {
            return await processRunner.RunAsync(DotnetExecutable, arguments, workingDirectory, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Win32Exception ex)
        {
            throw new PackageSourceException("Could not start the dotnet CLI. Is the .NET SDK installed and on PATH?", ex);
        }
    }

    /// <summary>The JSON body of the output, skipping stray lines (e.g. SDK notices) before it; null when there is none.</summary>
    public static string? JsonBody(string output)
    {
        int start = output.IndexOf('{');
        return start < 0 ? null : output[start..];
    }
}
