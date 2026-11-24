namespace NugetScope.Core.Solutions;

public sealed class SolutionLoadException(string solutionPath, string message, Exception? innerException = null)
    : Exception($"{message} ({solutionPath})", innerException)
{
    public string SolutionPath { get; } = solutionPath;
}
