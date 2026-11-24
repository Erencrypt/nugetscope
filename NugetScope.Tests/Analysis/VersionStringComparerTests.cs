using NugetScope.Core.Analysis;

namespace NugetScope.Tests.Analysis;

public sealed class VersionStringComparerTests
{
    [Fact]
    public void Order_SortsSemanticallyWithPrereleaseFirstAndUnparseableLast()
    {
        string[] versions = ["10.0.0", "not-a-version", "9.0.0", "10.0.0-preview.1", "9.0.10", "9.0.2"];

        var ordered = versions.Order(VersionStringComparer.Instance);

        Assert.Equal(["9.0.0", "9.0.2", "9.0.10", "10.0.0-preview.1", "10.0.0", "not-a-version"], ordered);
    }
}
