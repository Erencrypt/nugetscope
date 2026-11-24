namespace NugetScope.Core.Cli;

public interface IProcessRunner
{
    /// <summary>Runs a process to completion and captures its output. Cancelling kills the process tree.</summary>
    /// <exception cref="System.ComponentModel.Win32Exception">The executable could not be started.</exception>
    Task<ProcessResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        CancellationToken cancellationToken = default);
}
