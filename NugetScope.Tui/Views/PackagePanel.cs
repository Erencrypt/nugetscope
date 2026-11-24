using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Core.Solutions;
using NugetScope.Tui.Formatting;
using NugetScope.Tui.Rows;
using NugetScope.Tui.Theming;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace NugetScope.Tui.Views;

/// <summary>
/// Framed panel: a search field above the table. By package the table is flat; by project it is a stock
/// tree table (project rows expand to their packages, ←/→ collapse/expand).
/// </summary>
internal sealed class PackagePanel : FrameView
{
    private const int StatusColumn = 3;

    private readonly TextField _search;
    private readonly TableView _table;
    private IEnumerableTableSource<DashboardRow> _source;
    private PackageChecks _completedChecks;

    // The hidden tree behind the tree table (by project only) and its root rows.
    private TreeView<DashboardRow>? _tree;
    private IReadOnlyList<DashboardRow> _rows = [];

    // Projects the user collapsed; kept across searches and reloads (rows are rebuilt each time).
    private readonly HashSet<ProjectInfo> _collapsedProjects = [];

    public PackagePanel()
    {
        Title = StatusFormatter.Describe(PackageGrouping.ByPackage);

        var searchLabel = new Label { Text = "Search:", X = 0, Y = 1 };
        _search = new TextField { X = Pos.Right(searchLabel) + 1, Y = 1, Width = Dim.Fill() - 1 };
        _search.ValueChanged += (_, _) => SearchTextChanged?.Invoke(this, _search.Text);
        _search.KeyDown += (_, key) =>
        {
            // Unhandled, Up bubbles to Tabs, which selects the *previous* tab. Let the page go to its own header.
            if (key == Key.CursorUp && UpFromSearch is not null)
            {
                key.Handled = true;
                UpFromSearch.Invoke(this, EventArgs.Empty);
            }
        };

        _source = CreateFlatSource([]);
        _table = new TableView(_source)
        {
            X = 0,
            Y = Pos.Bottom(_search),
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            FullRowSelect = true,
        };
        _table.Style.GetOrCreateColumnStyle(StatusColumn).ColorGetter = GetStatusCellScheme;
        _table.ValueChanged += (_, _) => SelectedRowChanged?.Invoke(this, SelectedRow);

        Add(searchLabel, _search, _table);
    }

    public event EventHandler<string>? SearchTextChanged;

    /// <summary>Raised when Up is pressed in the search field (nothing above it inside the panel).</summary>
    public event EventHandler? UpFromSearch;

    /// <summary>Raised with the newly selected row, or null when nothing is selected.</summary>
    public event EventHandler<DashboardRow?>? SelectedRowChanged;

    public DashboardRow? SelectedRow =>
        _table.Value?.SelectedCell.Y is int row && row >= 0 && row < _source.Rows ? _source.GetObjectOnRow(row) : null;

    /// <param name="titleSuffix">Shown after the frame title, e.g. "5 of 12" while a search is active.</param>
    public void SetRows(
        IReadOnlyList<DashboardRow> rows,
        PackageGrouping grouping,
        PackageChecks completedChecks,
        string? titleSuffix)
    {
        string baseTitle = StatusFormatter.Describe(grouping);
        Title = titleSuffix is null ? baseTitle : $"{baseTitle} ({titleSuffix})";
        _completedChecks = completedChecks;

        RememberCollapsedProjects();
        (_source as IDisposable)?.Dispose(); // A TreeTableSource hooks the table's keys (and owns its tree); unhook it.
        _tree = null;
        _rows = rows;

        _source = grouping == PackageGrouping.ByProject ? CreateTreeSource(rows) : CreateFlatSource(rows);
        _table.Table = _source;
        _table.Update();
        SelectedRowChanged?.Invoke(this, SelectedRow);
    }

    public void FocusSearch() => _search.SetFocus();

    public void FocusTable() => _table.SetFocus();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            (_source as IDisposable)?.Dispose();
        }

        base.Dispose(disposing);
    }

    private EnumerableTableSource<DashboardRow> CreateFlatSource(IReadOnlyList<DashboardRow> rows) =>
        new(rows, new Dictionary<string, Func<DashboardRow, object>> { ["Package"] = r => r.Name }
            .Concat(DetailColumns(PackageGrouping.ByPackage))
            .ToDictionary());

    private TreeTableSource<DashboardRow> CreateTreeSource(IReadOnlyList<DashboardRow> rows)
    {
        var tree = _tree = new TreeView<DashboardRow>
        {
            TreeBuilder = new DelegateTreeBuilder<DashboardRow>(r => r.Children, r => r.Children.Count > 0),
            AspectGetter = r => r.Name,
        };
        tree.AddObjects(rows);
        tree.ExpandAll();

        foreach (ProjectRow row in rows.OfType<ProjectRow>().Where(r => _collapsedProjects.Contains(r.Project.Project)))
        {
            tree.Collapse(row);
        }

        return new TreeTableSource<DashboardRow>(_table, "Project", tree, DetailColumns(PackageGrouping.ByProject));
    }

    // Rows not currently shown (filtered out) keep their remembered state.
    private void RememberCollapsedProjects()
    {
        if (_tree is null)
        {
            return;
        }

        foreach (ProjectRow row in _rows.OfType<ProjectRow>())
        {
            if (_tree.IsExpanded(row))
            {
                _collapsedProjects.Remove(row.Project.Project);
            }
            else
            {
                _collapsedProjects.Add(row.Project.Project);
            }
        }
    }

    private Dictionary<string, Func<DashboardRow, object>> DetailColumns(PackageGrouping grouping) => new()
    {
        ["Version(s)"] = r => string.Join(", ", r.ResolvedVersions),
        ["Latest"] = r => r is ProjectRow ? "" : StatusFormatter.FormatLatest(r.LatestVersion, r.ResolvedVersions, _completedChecks),
        ["Status"] = r => StatusFormatter.FormatStatus(r.Status, r.HighestSeverity, _completedChecks),
        [grouping == PackageGrouping.ByProject ? "Packages" : "Projects"] = r => r.Count?.ToString() ?? "",
    };

    // Colour only the foreground of normal cells; the selected row keeps the theme's focus colours.
    private Scheme? GetStatusCellScheme(CellColorGetterArgs args)
    {
        if (args.RowIndex < 0 || args.RowIndex >= _source.Rows || args.RowScheme is not { } rowScheme)
        {
            return null;
        }

        DetailTone tone = Palette.ToneFor(_source.GetObjectOnRow(args.RowIndex).Status, _completedChecks);

        return new Scheme(rowScheme) { Normal = Palette.Apply(tone, rowScheme.Normal) };
    }
}
