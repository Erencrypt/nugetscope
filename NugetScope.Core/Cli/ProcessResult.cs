namespace NugetScope.Core.Cli;

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
