using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Tui.Formatting;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace NugetScope.Tui.Views;

/// <summary>Framed table of packages, one row per package id.</summary>
internal sealed class PackageTable : FrameView
{
    private const string BaseTitle = "Packages";

    private readonly TableView _table;
    private EnumerableTableSource<PackageSummary> _source;

    public PackageTable()
    {
        Title = BaseTitle;

        _source = CreateSource([], PackageChecks.None);
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

    /// <param name="titleSuffix">Shown after the frame title, e.g. the active filter.</param>
    public void SetPackages(IReadOnlyList<PackageSummary> packages, PackageChecks completedChecks, string? titleSuffix = null)
    {
        Title = titleSuffix is null ? BaseTitle : $"{BaseTitle} ({titleSuffix})";

        _source = CreateSource(packages, completedChecks);
        _table.Table = _source;
        _table.Update();
        RaiseSelectionChanged();
    }

    public void FocusTable() => _table.SetFocus();

    private static EnumerableTableSource<PackageSummary> CreateSource(
        IReadOnlyList<PackageSummary> packages,
        PackageChecks completedChecks) =>
        new(packages, new Dictionary<string, Func<PackageSummary, object>>
        {
            ["Package"] = p => p.Id,
            ["Version(s)"] = p => string.Join(", ", p.ResolvedVersions),
            ["Latest"] = p => StatusFormatter.FormatLatest(p, completedChecks),
            ["Status"] = p => StatusFormatter.FormatStatus(p, completedChecks),
            ["Projects"] = p => p.ProjectCount,
        });

    private void RaiseSelectionChanged() => SelectedPackageChanged?.Invoke(this, SelectedPackage);
}
