using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Core.Scanning;
using NugetScope.Tui.Formatting;
using NugetScope.Tui.Threading;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace NugetScope.Tui.Views;

internal sealed class MainWindow : Window
{
    private readonly IApplication _app;
    private readonly SolutionScanner _scanner;
    private readonly string _solutionPath;

    private readonly SpinnerView _spinner;
    private readonly Label _header;
    private readonly PackageTable _packageTable;
    private readonly DetailPane _detailPane;

    private CancellationTokenSource? _loadCancellation;
    private SolutionScan? _scan;

    public MainWindow(IApplication app, SolutionScanner scanner, string solutionPath)
    {
        _app = app;
        _scanner = scanner;
        _solutionPath = solutionPath;

        Title = $"nugetscope - {Path.GetFileName(solutionPath)}";

        MenuBar menuBar = CreateMenuBar();
        StatusBar statusBar = CreateStatusBar();

        _spinner = new SpinnerView { X = 0, Y = Pos.Bottom(menuBar), Visible = false };
        _header = new Label { X = 2, Y = Pos.Bottom(menuBar), Width = Dim.Fill() };

        _packageTable = new PackageTable
        {
            X = 0,
            Y = Pos.Bottom(_header),
            Width = Dim.Percent(55),
            Height = Dim.Fill(statusBar),
        };
        _packageTable.SelectedPackageChanged += (_, package) => ShowPackageDetails(package);

        _detailPane = new DetailPane
        {
            X = Pos.Right(_packageTable),
            Y = Pos.Bottom(_header),
            Width = Dim.Fill(),
            Height = Dim.Fill(statusBar),
        };

        Add(menuBar, _spinner, _header, _packageTable, _detailPane, statusBar);

        Initialized += (_, _) => StartLoad();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _loadCancellation?.Cancel();
            _loadCancellation?.Dispose();
        }

        base.Dispose(disposing);
    }

    private MenuBar CreateMenuBar() => new(
    [
        new MenuBarItem("_File",
        [
            new MenuItem("_Reload", "Scan the solution again", StartLoad, Key.F5),
            new MenuItem("_Problems", "Show restore and scan problems", ShowProblems, Key.P.WithCtrl),
            new MenuItem("_Quit", "Exit nugetscope", RequestStop, Key.Q.WithCtrl),
        ]),
    ]);

    private StatusBar CreateStatusBar() => new(
    [
        new Shortcut(Key.F5, "Reload", StartLoad, "Scan again"),
        new Shortcut(Key.P.WithCtrl, "Problems", ShowProblems, "Restore/scan problems"),
        new Shortcut(Key.Q.WithCtrl, "Quit", RequestStop, "Exit"),
    ]);

    // Must be called on the UI thread. Cancels any scan still in flight.
    private void StartLoad()
    {
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = new CancellationTokenSource();

        _ = LoadAsync(_loadCancellation.Token);
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        SetLoading(ScanStage.ReadingSolution);
        var progress = new UiProgress<ScanStage>(_app, SetLoading);

        try
        {
            // Task.Run keeps the CLI work and JSON parsing off the UI thread entirely.
            SolutionScan scan = await Task.Run(
                    () => _scanner.ScanAsync(_solutionPath, PackageScanOptions.Default, progress, cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);

            OnUiThread(cancellationToken, () => ShowScan(scan));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Superseded by a newer load or the window closed.
        }
        catch (Exception ex)
        {
            // Includes SolutionLoadException/PackageSourceException; never let a fire-and-forget task fail silently.
            OnUiThread(cancellationToken, () => ShowError(ex.Message));
        }
    }

    private void OnUiThread(CancellationToken cancellationToken, Action action) =>
        _app.Invoke(() =>
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                action();
            }
        });

    private void SetLoading(ScanStage stage)
    {
        _spinner.Visible = true;
        _spinner.AutoSpin = true;
        _header.Text = stage switch
        {
            ScanStage.ReadingSolution => "Reading solution...",
            ScanStage.ScanningPackages => "Restoring and listing packages (this can take a while)...",
            _ => "Loading...",
        };
    }

    private void StopLoading()
    {
        _spinner.AutoSpin = false;
        _spinner.Visible = false;
    }

    private void ShowScan(SolutionScan scan)
    {
        StopLoading();
        _scan = scan;

        IReadOnlyList<PackageSummary> packages = PackageSummaryBuilder.Build(scan.Packages);
        int failedProjects = scan.Packages.Projects.Count(p => p.HasErrors);

        _header.Text = $"{scan.Solution.Name} | {scan.Solution.Projects.Count} projects | {packages.Count} packages"
            + (failedProjects > 0 ? $" | {failedProjects} project(s) with errors (Ctrl+P)" : "");

        _packageTable.SetPackages(packages);

        if (packages.Count == 0)
        {
            _detailPane.ShowText(ProblemsFormatter.Count(scan.Packages) > 0
                ? ProblemsFormatter.Format(scan.Packages)
                : "No package references found.");
        }
    }

    private void ShowError(string message)
    {
        StopLoading();
        _scan = null;
        _header.Text = "Scan failed. Press F5 to retry.";
        _packageTable.SetPackages([]);
        _detailPane.ShowText(message);
    }

    private void ShowPackageDetails(PackageSummary? package)
    {
        if (package is not null)
        {
            _detailPane.ShowText(PackageDetailFormatter.Format(package));
        }
    }

    private void ShowProblems()
    {
        string text = _scan is null ? "No scan results yet." : ProblemsFormatter.Format(_scan.Packages);
        MessageBox.Query(_app, "Problems", text, "_OK");
    }
}
