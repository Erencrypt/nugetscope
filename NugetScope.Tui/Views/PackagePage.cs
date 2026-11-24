using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Tui.Formatting;
using Terminal.Gui.ViewBase;

namespace NugetScope.Tui.Views;

/// <summary>
/// One tab: packages matching a status filter, with its own search, table and detail pane.
/// Keeping the detail pane inside the page lets Tab move header → search → table → details natively.
/// </summary>
internal sealed class PackagePage : View
{
    private readonly PackagePanel _panel;
    private readonly DetailPane _details;

    private IReadOnlyList<PackageSummary> _allPackages = [];
    private PackageChecks _completedChecks;
    private string _searchText = "";

    public PackagePage(PackageStatusFilter status)
    {
        Status = status;
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;

        _panel = new PackagePanel { X = 0, Y = 0, Width = Dim.Percent(60), Height = Dim.Fill() };
        _panel.SearchTextChanged += (_, text) =>
        {
            _searchText = text;
            Refresh();
        };
        _panel.SelectedPackageChanged += (_, package) => ShowDetails(package);
        _panel.UpFromSearch += (_, _) => FocusOwnTabHeader();

        _details = new DetailPane { X = Pos.Right(_panel), Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };

        Add(_panel, _details);
        UpdateTitle(0);
    }

    public PackageStatusFilter Status { get; }

    /// <summary>Replaces the data shown on this page; keeps the page's search text.</summary>
    public void SetPackages(IReadOnlyList<PackageSummary> packages, PackageChecks completedChecks)
    {
        _allPackages = packages;
        _completedChecks = completedChecks;
        Refresh();
    }

    /// <summary>Shows a message instead of package details (e.g. scan errors or problems).</summary>
    public void ShowMessage(string message) => _details.ShowText(message);

    public void FocusSearch() => _panel.FocusSearch();

    public void FocusTable() => _panel.FocusTable();

    // Tabs makes the page itself a focus stop (e.g. Down from the tab header). Nothing on the page is usable
    // at that level, so hand focus to the search field. No stock TabStop setting avoids this stop (verified).
    protected override void OnHasFocusChanged(bool newHasFocus, View? previousFocusedView, View? focusedView)
    {
        base.OnHasFocusChanged(newHasFocus, previousFocusedView, focusedView);

        if (newHasFocus && Focused is null)
        {
            _panel.FocusSearch();
        }
    }

    private void Refresh()
    {
        var statusFilter = new PackageFilter(Status: Status);
        IReadOnlyList<PackageSummary> inTab = statusFilter.Apply(_allPackages);
        IReadOnlyList<PackageSummary> visible = (statusFilter with { SearchText = _searchText }).Apply(inTab);

        UpdateTitle(inTab.Count);
        _panel.SetPackages(visible, _completedChecks, string.IsNullOrWhiteSpace(_searchText) ? null : $"{visible.Count} of {inTab.Count}");

        if (visible.Count == 0)
        {
            _details.ShowText(_allPackages.Count == 0 ? "" : "No packages match this tab and search.");
        }
    }

    // Tabs draws each page's header in the page's border; focusing it keeps this tab selected.
    private void FocusOwnTabHeader() => (Border.View as BorderView)?.TitleView?.SetFocus();

    private void UpdateTitle(int count) => Title = $"{StatusFormatter.Describe(Status)} {count}";

    private void ShowDetails(PackageSummary? package)
    {
        if (package is not null)
        {
            _details.ShowLines(PackageDetailFormatter.Format(package, _completedChecks));
        }
    }
}
