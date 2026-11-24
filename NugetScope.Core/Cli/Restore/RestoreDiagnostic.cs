using NugetScope.Core.Packages;

namespace NugetScope.Core.Cli.Restore;

/// <summary>One error/warning line from <c>dotnet restore</c> output.</summary>
/// <param name="FilePath">Normalized path of the file the diagnostic is about (usually a project), or null.</param>
internal sealed record RestoreDiagnostic(string? FilePath, ProblemLevel Level, string Code, string Message)
{
    public PackageProblem ToProblem() => new(Level, Message, Code);
}
