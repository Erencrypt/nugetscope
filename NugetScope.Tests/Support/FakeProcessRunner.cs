using NugetScope.Core.Cli;

namespace NugetScope.Tests.Support;

/// <summary>Returns canned results keyed by the first CLI argument (e.g. "restore", "list").</summary>
internal sealed class FakeProcessRunner : IProcessRunner
{
    private readonly Dictionary<string, Func<ProcessResult>> _responses = new(StringComparer.Ordinal);

    public List<IReadOnlyList<string>> Calls { get; } = [];

    public FakeProcessRunner On(string command, ProcessResult result) => On(command, () => result);

    public FakeProcessRunner On(string command, Func<ProcessResult> result)
    {
        _responses[command] = result;
        return this;
    }

    public Task<ProcessResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        CancellationToken cancellationToken = default)
    {
        Calls.Add(arguments);

        return _responses.TryGetValue(arguments[0], out var response)
            ? Task.FromResult(response())
            : throw new InvalidOperationException($"No fake response for 'dotnet {arguments[0]}'.");
    }
}
