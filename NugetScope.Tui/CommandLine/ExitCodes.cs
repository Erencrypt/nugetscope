namespace NugetScope.Tui.CommandLine;

internal static class ExitCodes
{
    public const int Success = 0;

    /// <summary>Bad arguments, no solution, scan failure, or (with --fail-on) incomplete data.</summary>
    public const int Error = 1;

    /// <summary>A package matched --fail-on.</summary>
    public const int PolicyFailed = 2;
}
