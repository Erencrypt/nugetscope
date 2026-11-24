using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Tui.Formatting;
using NugetScope.Tui.Theming;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace NugetScope.Tui.Views;

/// <summary>Framed "Packages" panel: a search field above the package table.</summary>
internal sealed class PackagePanel : FrameView
{
    private const string BaseTitle = "Packages";
    private const int StatusColumn = 3;

    private readonly TextField _search;
    private readonly TableView _table;
    private EnumerableTableSource<PackageSummary> _source;
    private PackageChecks _completedChecks;

    public PackagePanel()
    {
        Title = BaseTitle;

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

        _source = CreateSource([]);
        _table = new TableView(_source)
        {
            X = 0,
            Y = Pos.Bottom(_search),
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            FullRowSelect = true,
        };
        _table.Style.GetOrCreateColumnStyle(StatusColumn).ColorGetter = GetStatusCellScheme;
        _table.ValueChanged += (_, _) => SelectedPackageChanged?.Invoke(this, SelectedPackage);

        Add(searchLabel, _search, _table);
    }

    public event EventHandler<string>? SearchTextChanged;

    /// <summary>Raised when Up is pressed in the search field (nothing above it inside the panel).</summary>
    public event EventHandler? UpFromSearch;

    /// <summary>Raised with the newly selected package, or null when nothing is selected.</summary>
    public event EventHandler<PackageSummary?>? SelectedPackageChanged;

    public PackageSummary? SelectedPackage =>
        _table.Value?.SelectedCell.Y is int row && row >= 0 && row < _source.Rows ? _source.GetObjectOnRow(row) : null;

    /// <param name="titleSuffix">Shown after the frame title, e.g. "5 of 12" while a search is active.</param>
    public void SetPackages(IReadOnlyList<PackageSummary> packages, PackageChecks completedChecks, string? titleSuffix)
    {
        Title = titleSuffix is null ? BaseTitle : $"{BaseTitle} ({titleSuffix})";
        _completedChecks = completedChecks;

        _source = CreateSource(packages);
        _table.Table = _source;
        _table.Update();
        SelectedPackageChanged?.Invoke(this, SelectedPackage);
    }

    public void FocusSearch() => _search.SetFocus();

    public void FocusTable() => _table.SetFocus();

    private EnumerableTableSource<PackageSummary> CreateSource(IReadOnlyList<PackageSummary> packages) =>
        new(packages, new Dictionary<string, Func<PackageSummary, object>>
        {
            ["Package"] = p => p.Id,
            ["Version(s)"] = p => string.Join(", ", p.ResolvedVersions),
            ["Latest"] = p => StatusFormatter.FormatLatest(p, _completedChecks),
            ["Status"] = p => StatusFormatter.FormatStatus(p, _completedChecks),
            ["Projects"] = p => p.ProjectCount,
        });

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
