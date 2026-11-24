using NugetScope.Core.Packages;
using NugetScope.Core.Scanning;
using NugetScope.Core.Solutions;

namespace NugetScope.Tests.Scanning;

public sealed class SolutionScannerTests
{
    private static readonly SolutionInfo Solution = new("/repo/App.slnx", [new ProjectInfo("Api", "/repo/Api/Api.csproj", null)]);

    [Fact]
    public async Task ScanAsync_ReadsSolutionThenScansPackages_ReportingStages()
    {
        var expectedPackages = new PackageScanResult([], []);
        var source = new StubPackageSource(expectedPackages);
        var stages = new List<ScanStage>();
        var scanner = new SolutionScanner(new StubSolutionReader(Solution), source);

        var scan = await scanner.ScanAsync(
            "/repo/App.slnx",
            PackageScanOptions.Default,
            new SynchronousProgress<ScanStage>(stages.Add),
            TestContext.Current.CancellationToken);

        Assert.Same(Solution, scan.Solution);
        Assert.Same(expectedPackages, scan.Packages);
        Assert.Same(Solution, source.ScannedSolution);
        // The scanner reports its own stage and passes the reporter through to the package source.
        Assert.Equal([ScanStage.ReadingSolution, ScanStage.ListingPackages], stages);
    }

    [Fact]
    public async Task ScanAsync_ReaderFails_DoesNotScanPackages()
    {
        var source = new StubPackageSource(new PackageScanResult([], []));
        var scanner = new SolutionScanner(new FailingSolutionReader(), source);

        await Assert.ThrowsAsync<SolutionLoadException>(
            () => scanner.ScanAsync("/repo/App.slnx", PackageScanOptions.Default, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Null(source.ScannedSolution);
    }

    private sealed class StubSolutionReader(SolutionInfo solution) : ISolutionReader
    {
        public Task<SolutionInfo> ReadAsync(string solutionPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(solution);
    }

    private sealed class FailingSolutionReader : ISolutionReader
    {
        public Task<SolutionInfo> ReadAsync(string solutionPath, CancellationToken cancellationToken = default) =>
            throw new SolutionLoadException(solutionPath, "broken");
    }

    private sealed class StubPackageSource(PackageScanResult result) : IPackageSource
    {
        public SolutionInfo? ScannedSolution { get; private set; }

        public Task<PackageScanResult> ScanAsync(
            SolutionInfo solution,
            PackageScanOptions options,
            IProgress<ScanStage>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ScannedSolution = solution;
            progress?.Report(ScanStage.ListingPackages);
            return Task.FromResult(result);
        }
    }

    // Progress<T> posts to the thread pool; this reports inline so assertions see every stage.
    private sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }
}
