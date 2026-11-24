using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Core.Scanning;
using NugetScope.Tui.Formatting;
using NugetScope.Tui.State;
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

    private readonly DashboardState _state = new();
    private readonly Tabs _tabs;
    private readonly IReadOnlyList<PackagePage> _pages;

    private CancellationTokenSource? _loadCancellation;
    private readonly TextSpinner _spinner;
    private string _headerText = "";

    public MainWindow(IApplication app, SolutionScanner scanner, string solutionPath)
    {
        _app = app;
        _scanner = scanner;
        _solutionPath = solutionPath;

        // A SpinnerView can't sit inside a border title, so the title text carries the spinner frame.
        _spinner = new TextSpinner(app);
        _spinner.Changed += (_, _) => UpdateTitle();

        HeaderText = Path.GetFileName(solutionPath);

        _pages = Enum.GetValues<PackageStatusFilter>().Select(status => new PackagePage(_state, status)).ToList();

        MenuBar menuBar = CreateMenuBar();
        StatusBar statusBar = CreateStatusBar();

        _tabs = new Tabs { X = 0, Y = Pos.Bottom(menuBar), Width = Dim.Fill(), Height = Dim.Fill(statusBar) };
        _tabs.Add([.. _pages]);

        Add(menuBar, _tabs, statusBar);

        Initialized += (_, _) =>
        {
            _tabs.Value = _pages[0];
            _pages[0].FocusTable();
            StartLoad();
        };
    }

    // Solution summary and loading status live in the window's top border title.
    private string HeaderText
    {
        set
        {
            _headerText = value;
            UpdateTitle();
        }
    }

    private PackagePage CurrentPage => _tabs.Value as PackagePage ?? _pages[0];

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _loadCancellation?.Cancel();
            _loadCancellation?.Dispose();
            _spinner.Dispose();
        }

        base.Dispose(disposing);
    }

    private void UpdateTitle() =>
        Title = _spinner.IsSpinning
            ? $"nugetscope {_spinner.Frame} {_headerText}"
            : $"nugetscope - {_headerText}";


    private MenuBar CreateMenuBar() => new(
    [
        new MenuBarItem("_File",
        [
            new MenuItem("_Reload", "Scan the solution again", StartLoad, Key.F5),
            new MenuItem("_Problems", "Show restore and scan problems", ShowProblems, Key.P.WithCtrl),
            new MenuItem("_Quit", "Exit nugetscope", RequestStop, Key.Q.WithCtrl),
        ]),
        new MenuBarItem("_View",
        [
            new MenuItem("_Search", "Search this tab", () => CurrentPage.FocusSearch(), Key.F.WithCtrl),
            new MenuItem("_Group", "Switch between by package and by project", _state.ToggleGrouping, Key.G.WithCtrl),
            new MenuItem("_Next tab", "Select the next tab", SelectNextTab, Key.F3),
            .. _pages.Select(page => new MenuItem(
                StatusFormatter.Describe(page.Status),
                $"Show the {StatusFormatter.Describe(page.Status)} tab",
                () => SelectPage(page),
                Key.Empty)),
        ]),
    ]);

    private StatusBar CreateStatusBar() => new(
    [
        new Shortcut(Key.F5, "Reload", StartLoad),
        new Shortcut(Key.F3, "Next tab", SelectNextTab),
        new Shortcut(Key.F.WithCtrl, "Search", () => CurrentPage.FocusSearch()),
        new Shortcut(Key.G.WithCtrl, "Group", _state.ToggleGrouping),
        new Shortcut(Key.P.WithCtrl, "Problems", ShowProblems),
        new Shortcut(Key.Q.WithCtrl, "Quit", RequestStop),
    ]);

    private void SelectNextTab() => SelectPage(_pages[(_pages.ToList().IndexOf(CurrentPage) + 1) % _pages.Count]);

    private void SelectPage(PackagePage page)
    {
        _tabs.Value = page;
        page.FocusTable();
    }

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
        _spinner.Start();
        HeaderText = stage switch
        {
            ScanStage.ReadingSolution => "Reading solution...",
            ScanStage.Restoring => "Restoring packages...",
            ScanStage.ListingPackages => "Listing packages and checking for updates, deprecations and vulnerabilities...",
            _ => "Loading...",
        };
    }

    private void ShowScan(SolutionScan scan)
    {
        _spinner.Stop();
        _state.Load(scan);

        int packageCount = _state.Packages.Count;
        int failedProjects = scan.Packages.Projects.Count(p => p.HasErrors);

        HeaderText = string.Join(" | ", new[]
        {
            scan.Solution.Name,
            $"{scan.Solution.Projects.Count} projects",
            $"{packageCount} packages",
            failedProjects > 0 ? $"{failedProjects} project(s) with errors (Ctrl+P)" : "",
        }.Where(part => part.Length > 0));

        if (packageCount == 0)
        {
            _pages[0].ShowMessage(ProblemsFormatter.Count(scan.Packages) > 0
                ? ProblemsFormatter.Format(scan.Packages)
                : "No package references found.");
        }
    }

    private void ShowError(string message)
    {
        _spinner.Stop();
        _state.Clear();
        HeaderText = "Scan failed. Press F5 to retry.";

        _pages[0].ShowMessage(message);
    }

    private void ShowProblems()
    {
        string text = _state.Scan is null ? "No scan results yet." : ProblemsFormatter.Format(_state.Scan.Packages);
        MessageBox.Query(_app, "Problems", text, "_OK");
    }
}
