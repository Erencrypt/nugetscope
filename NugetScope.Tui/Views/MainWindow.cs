using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Core.Scanning;
using NugetScope.Core.Solutions;
using NugetScope.Tui.Formatting;
using NugetScope.Tui.State;
using NugetScope.Tui.Threading;
using NugetScope.Tui.Updates;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace NugetScope.Tui.Views;

internal sealed class MainWindow : Window
{
    private readonly IApplication _app;
    private readonly SolutionScanner _scanner;
    private readonly string _solutionPath;
    private readonly PackageUpdateFlow _updateFlow;

    private readonly DashboardState _state = new();
    private readonly Tabs _tabs;
    private readonly IReadOnlyList<PackagePage> _pages;

    private CancellationTokenSource? _loadCancellation;
    private bool _tabHeadersStale;
    private readonly TextSpinner _spinner;
    private string _headerText = "";
    private string _summaryText = "";
    private bool _isLoading;
    private bool _isUpdating;

    public MainWindow(IApplication app, SolutionScanner scanner, PackageUpdateFlow updateFlow, string solutionPath)
    {
        _app = app;
        _scanner = scanner;
        _updateFlow = updateFlow;
        _solutionPath = solutionPath;

        // A SpinnerView can't sit inside a border title, so the title text carries the spinner frame.
        _spinner = new TextSpinner(app);
        _spinner.Changed += (_, _) => UpdateTitle();

        HeaderText = Path.GetFileName(solutionPath);

        // Only the top border line, which carries the title; the tabs frame already outlines the content.
        Border.Thickness = new Thickness(0, 1, 0, 0);

        _pages = Enum.GetValues<PackageStatusFilter>().Select(status => new PackagePage(_state, status)).ToList();

        MenuBar menuBar = CreateMenuBar();
        StatusBar statusBar = CreateStatusBar();

        _tabs = new Tabs { X = 0, Y = Pos.Bottom(menuBar), Width = Dim.Fill(), Height = Dim.Fill(statusBar) };
        _tabs.Add([.. _pages]);

        // Tabs gives each page a 1-cell left/right/bottom border (no setting for it); keep only the header rows on top.
        // Verified to survive tab switches, reloads and resizes, with navigation unchanged.
        foreach (PackagePage page in _pages)
        {
            page.Border.Thickness = page.Border.Thickness with { Left = 0, Right = 0, Bottom = 0 };
            IgnoreUpOnTabHeader(page);
            page.UpdateRequested += (_, _) => StartUpdate();
        }

        // Tabs binds Up to "previous tab". Without it, Up falls through to Terminal.Gui's app-wide Up
        // (previous tab stop), so Up on the table's top row goes to the search field.
        _tabs.KeyBindings.Remove(Key.CursorUp);

        Add(menuBar, _tabs, statusBar);
        KeepTabHeadersInSync();

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


    // A tab header binds Up itself and forwards it to Tabs ("previous tab"). Up on a header should do nothing,
    // so consume it there. The header view is created lazily, hence the SubViewAdded hook.
    private static void IgnoreUpOnTabHeader(View page)
    {
        if (page.Border.View is not BorderView border)
        {
            return;
        }

        if (border.TitleView is { } existing)
        {
            ConsumeUp(existing);
        }

        border.SubViewAdded += (_, e) =>
        {
            if (e.SubView is TitleView header)
            {
                ConsumeUp(header);
            }
        };

        static void ConsumeUp(View header) => header.KeyDown += (_, key) =>
        {
            if (key == Key.CursorUp)
            {
                key.Handled = true;
            }
        };
    }

    // Tabs positions its headers before the pages re-measure their titles, so after a title (count) changes the
    // headers keep the old widths until the next layout, leaving artifacts. Lay Tabs out once more when that happens.
    private void KeepTabHeadersInSync()
    {
        foreach (PackagePage page in _pages)
        {
            page.TitleChanged += (_, _) => _tabHeadersStale = true;
        }

        _tabs.SubViewsLaidOut += (_, _) =>
        {
            if (!_tabHeadersStale)
            {
                return;
            }

            _tabHeadersStale = false;
            _tabs.SetNeedsLayout();
            _tabs.Layout();
        };
    }

    private MenuBar CreateMenuBar() => new(
    [
        new MenuBarItem("_File",
        [
            new MenuItem("_Reload", "Scan the solution again", StartLoad, Key.F5),
            new MenuItem("_Problems", "Show restore and scan problems", ShowProblems, Key.P.WithCtrl),
            new MenuItem("_Quit", "Exit nugetscope", RequestStop, Key.Q.WithCtrl),
        ]),
        new MenuBarItem("_Package",
        [
            new MenuItem("_Update...", "Update the selected package, after confirming the exact changes", StartUpdate, Key.U.WithCtrl),
        ]),
        new MenuBarItem("_View",
        [
            new MenuItem("_Search", "Search this tab", () => CurrentPage.FocusSearch(), Key.F.WithCtrl),
            new MenuItem("_Details", "Focus the details, or go back to the list", () => CurrentPage.ToggleDetailsFocus(), Key.O.WithCtrl),
            new MenuItem("_Group", "Switch between by package and by project", _state.ToggleGrouping, Key.G.WithCtrl),
            // Keys of menu-only items fire app-wide only with BindKeyToApplication (the others come from the status bar).
            new MenuItem("_Transitive packages", "Show or hide packages that come in through other packages", ToggleTransitive, Key.T.WithCtrl)
            {
                BindKeyToApplication = true,
            },
            new MenuItem("_Next tab", "Select the next tab", SelectNextTab, Key.F3) { BindKeyToApplication = true },
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
        new Shortcut(Key.F.WithCtrl, "Search", () => CurrentPage.FocusSearch()),
        new Shortcut(Key.U.WithCtrl, "Update", StartUpdate),
        new Shortcut(Key.P.WithCtrl, "Problems", ShowProblems),
        new Shortcut(Key.O.WithCtrl, "Details", () => CurrentPage.ToggleDetailsFocus()),
        new Shortcut(Key.G.WithCtrl, "Group", _state.ToggleGrouping),
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

        _ = LoadAsync(_state.ShowTransitive, _loadCancellation.Token);
    }

    // The first time transitive packages are shown, the scan has to run again with them included.
    private void ToggleTransitive()
    {
        if (_state.ToggleTransitive() || (_isLoading && _state.ShowTransitive))
        {
            StartLoad();
        }
    }

    private async Task LoadAsync(bool includeTransitive, CancellationToken cancellationToken)
    {
        var options = PackageScanOptions.Default with { IncludeTransitive = includeTransitive };
        SetLoading(ScanStage.ReadingSolution);
        var progress = new UiProgress<ScanStage>(_app, SetLoading);

        try
        {
            // Task.Run keeps the CLI work and JSON parsing off the UI thread entirely.
            SolutionScan scan = await Task.Run(
                    () => _scanner.ScanAsync(_solutionPath, options, progress, cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);

            OnUiThread(cancellationToken, () => ShowScan(scan, includeTransitive));
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
        _isLoading = true;
        _spinner.Start();
        HeaderText = stage switch
        {
            ScanStage.ReadingSolution => "Reading solution...",
            ScanStage.Restoring => "Restoring packages...",
            ScanStage.ListingPackages => "Listing packages and checking for updates, deprecations and vulnerabilities...",
            _ => "Loading...",
        };
    }

    private void ShowScan(SolutionScan scan, bool includesTransitive)
    {
        _isLoading = false;
        _spinner.Stop();
        _state.Load(scan, includesTransitive);

        int packageCount = _state.Packages.Count;
        int failedProjects = scan.Packages.Projects.Count(p => p.HasErrors);

        HeaderText = _summaryText = string.Join(" | ", new[]
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
        _isLoading = false;
        _spinner.Stop();
        _state.Clear();
        HeaderText = _summaryText = "Scan failed. Press F5 to retry.";

        _pages[0].ShowMessage(message);
    }

    // The flow runs in the background and shows its own dialogs; a successful update rescans (and restores).
    private void StartUpdate()
    {
        if (_isLoading || _isUpdating)
        {
            MessageBox.Query(_app, "Update", "Wait for the current scan or update to finish.", "_OK");
            return;
        }

        if (CurrentPage.SelectedRow is not { Package: { } package } row)
        {
            MessageBox.Query(_app, "Update", "Select a package to update.", "_OK");
            return;
        }

        if (row.UpdateProjects.Count == 0)
        {
            MessageBox.Query(_app, "Update", $"{package.Id} is only a transitive dependency here. Add a direct reference to update it.", "_OK");
            return;
        }

        _isUpdating = true;
        _ = UpdateAsync(package, row.UpdateProjects);
    }

    private async Task UpdateAsync(PackageSummary package, IReadOnlyList<ProjectInfo> projects)
    {
        string solutionDirectory = Path.GetDirectoryName(Path.GetFullPath(_solutionPath))!;
        bool applied = await Task.Run(() => _updateFlow.RunAsync(package, projects, solutionDirectory, SetBusy)).ConfigureAwait(false);

        _app.Invoke(() =>
        {
            _isUpdating = false;
            if (applied)
            {
                StartLoad();
            }
        });
    }

    // Busy text with the spinner, or back to the solution summary when null.
    private void SetBusy(string? text)
    {
        if (text is null)
        {
            _spinner.Stop();
            HeaderText = _summaryText;
        }
        else
        {
            _spinner.Start();
            HeaderText = text;
        }
    }

    private void ShowProblems()
    {
        string text = _state.Scan is null ? "No scan results yet." : ProblemsFormatter.Format(_state.Scan.Packages);
        MessageBox.Query(_app, "Problems", text, "_OK");
    }
}
