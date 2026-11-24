using NugetScope.Core.Cli.ListPackage;
using NugetScope.Core.Packages;

namespace NugetScope.Tests.Cli;

public sealed class ListPackageArgumentsTests
{
    [Theory]
    // Query passed by name: ListPackageQuery is internal and can't appear in a public test signature.
    [InlineData(nameof(ListPackageQuery.Packages), null)]
    [InlineData(nameof(ListPackageQuery.Outdated), "--outdated")]
    [InlineData(nameof(ListPackageQuery.Deprecated), "--deprecated")]
    [InlineData(nameof(ListPackageQuery.Vulnerable), "--vulnerable")]
    public void Build_AddsStatusFlagAndPinnedJsonOptions(string queryName, string? expectedFlag)
    {
        var query = Enum.Parse<ListPackageQuery>(queryName);

        var args = ListPackageArguments.Build("/repo/App.slnx", query, includeTransitive: false);

        Assert.Equal(["list", "/repo/App.slnx", "package"], args.Take(3));
        Assert.Equal("json", args[args.ToList().IndexOf("--format") + 1]);
        Assert.Equal("1", args[args.ToList().IndexOf("--output-version") + 1]);
        Assert.Contains("--no-restore", args);
        Assert.DoesNotContain("--include-transitive", args);

        var statusFlags = args.Where(a => a is "--outdated" or "--deprecated" or "--vulnerable").ToList();
        Assert.Equal(expectedFlag is null ? [] : [expectedFlag], statusFlags);
    }

    [Fact]
    public void Build_IncludeTransitive_AddsFlag()
    {
        Assert.Contains("--include-transitive", ListPackageArguments.Build("/repo/App.slnx", ListPackageQuery.Vulnerable, includeTransitive: true));
    }

    [Fact]
    public void StatusQueries_MapsRequestedChecksOnly()
    {
        Assert.Empty(ListPackageArguments.StatusQueries(PackageChecks.None));
        Assert.Equal(
            [ListPackageQuery.Outdated, ListPackageQuery.Vulnerable],
            ListPackageArguments.StatusQueries(PackageChecks.Outdated | PackageChecks.Vulnerable).Select(q => q.Query));
        Assert.Equal(3, ListPackageArguments.StatusQueries(PackageChecks.All).Count);
    }
}
