namespace NugetScope.Core.Solutions;

public interface ISolutionReader
{
    /// <summary>Reads a .sln or .slnx file.</summary>
    /// <exception cref="SolutionLoadException">The file is missing, unsupported or malformed.</exception>
    Task<SolutionInfo> ReadAsync(string solutionPath, CancellationToken cancellationToken = default);
}
