using NugetScope.Core.Analysis;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace NugetScope.Tui.Views;

/// <summary>Framed table of packages, one row per package id.</summary>
internal sealed class PackageTable : FrameView
{
    private static readonly Dictionary<string, Func<PackageSummary, object>> Columns = new()
    {
        ["Package"] = s => s.Id,
        ["Version(s)"] = s => string.Join(", ", s.ResolvedVersions),
        ["Projects"] = s => s.ProjectCount,
    };

    private readonly TableView _table;
    private EnumerableTableSource<PackageSummary> _source;

    public PackageTable()
    {
        Title = "Packages";

        _source = CreateSource([]);
        _table = new TableView(_source)
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            FullRowSelect = true,
        };
        _table.ValueChanged += (_, _) => RaiseSelectionChanged();

        Add(_table);
    }

    /// <summary>Raised with the newly selected package, or null when nothing is selected.</summary>
    public event EventHandler<PackageSummary?>? SelectedPackageChanged;

    public PackageSummary? SelectedPackage =>
        _table.Value?.SelectedCell.Y is int row && row >= 0 && row < _source.Rows
            ? _source.GetObjectOnRow(row)
            : null;

    public void SetPackages(IReadOnlyList<PackageSummary> packages)
    {
        _source = CreateSource(packages);
        _table.Table = _source;
        _table.Update();
        RaiseSelectionChanged();
    }

    private static EnumerableTableSource<PackageSummary> CreateSource(IReadOnlyList<PackageSummary> packages) =>
        new(packages, Columns);

    private void RaiseSelectionChanged() => SelectedPackageChanged?.Invoke(this, SelectedPackage);
}
