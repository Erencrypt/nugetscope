using NugetScope.Core.Analysis;
using NugetScope.Tui.Formatting;
using NugetScope.Tui.Rows;
using NugetScope.Tui.State;
using Terminal.Gui.ViewBase;

namespace NugetScope.Tui.Views;

/// <summary>
/// One tab: rows matching a status filter, with its own search, table and detail pane.
/// Keeping the detail pane inside the page lets Tab move header → search → table → details natively.
/// </summary>
internal sealed class PackagePage : View
{
    private readonly DashboardState _state;
    private readonly PackagePanel _panel;
    private readonly DetailPane _details;

    private string _searchText = "";

    public PackagePage(DashboardState state, PackageStatusFilter status)
    {
        _state = state;
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
        _panel.SelectedRowChanged += (_, row) => ShowDetails(row);
        _panel.UpFromSearch += (_, _) => FocusOwnTabHeader();

        _details = new DetailPane { X = Pos.Right(_panel), Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };

        Add(_panel, _details);
        UpdateTitle(0);

        _state.Changed += OnStateChanged;
    }

    public PackageStatusFilter Status { get; }

    public DashboardRow? SelectedRow => _panel.SelectedRow;

    /// <summary>Shows a message instead of details (e.g. scan errors or problems).</summary>
    public void ShowMessage(string message) => _details.ShowText(message);

    public void FocusSearch() => _panel.FocusSearch();

    public void FocusTable() => _panel.FocusTable();

    /// <summary>Moves focus into the details (to scroll long text), or back to the table when already there.</summary>
    public void ToggleDetailsFocus()
    {
        if (_details.HasFocus)
        {
            _panel.FocusTable();
        }
        else
        {
            _details.FocusList();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _state.Changed -= OnStateChanged;
        }

        base.Dispose(disposing);
    }

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

    private void OnStateChanged(object? sender, EventArgs e) => Refresh();

    private void Refresh()
    {
        var statusFilter = new PackageFilter(Status: Status);
        IReadOnlyList<DashboardRow> inTab = _state.RowsFor(statusFilter);
        IReadOnlyList<DashboardRow> visible = string.IsNullOrWhiteSpace(_searchText)
            ? inTab
            : _state.RowsFor(statusFilter with { SearchText = _searchText });

        UpdateTitle(inTab.Count);
        _panel.SetRows(
            visible,
            _state.Grouping,
            _state.CompletedChecks,
            string.IsNullOrWhiteSpace(_searchText) ? null : $"{visible.Count} of {inTab.Count}");

        if (visible.Count == 0)
        {
            _details.ShowText(_state.Packages.Count == 0 ? "" : "Nothing matches this tab and search.");
        }
    }

    // Tabs draws each page's header in the page's border; focusing it keeps this tab selected.
    private void FocusOwnTabHeader() => (Border.View as BorderView)?.TitleView?.SetFocus();

    // Tabs sizes the header from the title during layout; a new title alone isn't re-measured until the next layout.
    private void UpdateTitle(int count)
    {
        Title = $"{StatusFormatter.Describe(Status)} {count}";
        SetNeedsLayout();
    }

    private void ShowDetails(DashboardRow? row)
    {
        switch (row)
        {
            case ProjectRow project:
                // Details show the whole project, not just the packages this tab lets through.
                ProjectSummary full = _state.FindProject(project.Project.Project) ?? project.Project;
                _details.ShowLines(ProjectDetailFormatter.Format(full, _state.ProblemsFor(full.Project), _state.CompletedChecks));
                break;
            case { Package: { } package }:
                _details.ShowLines(PackageDetailFormatter.Format(package, _state.CompletedChecks));
                break;
        }
    }
}
