using NugetScope.Core.Unused;

namespace NugetScope.Tests.Unused;

public sealed class SourceIdentifiersTests
{
    [Fact]
    public void Collects_dotted_names_and_all_their_prefixes()
    {
        var identifiers = new HashSet<string>();

        SourceIdentifiers.AddFrom("global using Serilog.Sinks;\nvar x = global::Polly.Retry.RetryPolicy.Handle();", identifiers);

        Assert.Superset(new HashSet<string> { "Serilog", "Serilog.Sinks", "Polly", "Polly.Retry", "Polly.Retry.RetryPolicy" }, identifiers);
        Assert.DoesNotContain("Sinks", identifiers); // Only prefixes count: a namespace must start where the name starts.
    }
}
