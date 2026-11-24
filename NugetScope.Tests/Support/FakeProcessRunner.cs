using NugetScope.Core.Cli;

namespace NugetScope.Tests.Support;

/// <summary>Returns canned results for dotnet invocations, matched on the command and (for list) the status flag.</summary>
internal sealed class FakeProcessRunner : IProcessRunner
{
    private static readonly string[] StatusFlags = ["--outdated", "--deprecated", "--vulnerable"];

    private readonly List<(Func<IReadOnlyList<string>, bool> Matches, Func<ProcessResult> Respond)> _responses = [];
    private readonly Lock _lock = new();

    public List<IReadOnlyList<string>> Calls { get; } = [];

    public FakeProcessRunner OnRestore(ProcessResult result) =>
        On(args => args[0] == "restore", () => result);

    public FakeProcessRunner OnRestore(Func<ProcessResult> result) =>
        On(args => args[0] == "restore", result);

    /// <summary>The base <c>dotnet list package</c> call (no status flag).</summary>
    public FakeProcessRunner OnList(ProcessResult result) =>
        On(args => args[0] == "list" && !args.Any(StatusFlags.Contains), () => result);

    /// <param name="statusFlag">E.g. <c>--outdated</c>.</param>
    public FakeProcessRunner OnList(string statusFlag, ProcessResult result) =>
        On(args => args[0] == "list" && args.Contains(statusFlag), () => result);

    public FakeProcessRunner On(Func<IReadOnlyList<string>, bool> matches, Func<ProcessResult> respond)
    {
        _responses.Add((matches, respond));
        return this;
    }

    public Task<ProcessResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            Calls.Add(arguments);
        }

        var response = _responses.LastOrDefault(r => r.Matches(arguments)).Respond
            ?? throw new InvalidOperationException($"No fake response for 'dotnet {string.Join(' ', arguments)}'.");

        return Task.FromResult(response());
    }
}
