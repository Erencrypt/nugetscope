using System.Collections.ObjectModel;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace NugetScope.Tui.Dialogs;

/// <summary>Lets the user choose the version to update a package to. Result is the version, or null if cancelled.</summary>
internal sealed class VersionPickerDialog : Dialog<string>
{
    private const int MaxVisibleItems = 15;

    private readonly IReadOnlyList<string> _versions;
    private readonly ListView _list;
    private readonly Button _cancelButton;

    /// <param name="description">Shown above the list (what will be updated).</param>
    /// <param name="labels">One display line per version, same order as <paramref name="versions"/>.</param>
    public VersionPickerDialog(
        string packageId,
        string description,
        IReadOnlyList<string> versions,
        IReadOnlyList<string> labels,
        int selectedIndex)
    {
        _versions = versions;
        Title = $"Update {packageId}";

        var descriptionLabel = new Label { Text = description, X = 0, Y = 0 };

        var items = new ObservableCollection<string>(labels);
        int widestLabel = labels.Max(l => l.Length);

        _list = new ListView
        {
            Y = Pos.Bottom(descriptionLabel) + 1,
            Width = Dim.Fill(0, widestLabel + 2),
            Height = Math.Min(labels.Count, MaxVisibleItems),
        };
        _list.SetSource(items);
        _list.Value = selectedIndex;
        Add(descriptionLabel, _list);

        _cancelButton = new Button { Text = "_Cancel" };
        AddButton(_cancelButton);
        AddButton(new Button { Text = "_Next" }); // Last button added is the default.

        Initialized += (_, _) => _list.SetFocus();
    }

    // Dialog<T> stops on any Accept and treats a null Result as cancel. Enter in the list and the Next
    // button both arrive here; only the Cancel button must leave Result unset.
    protected override bool OnAccepting(CommandEventArgs args)
    {
        View? source = null;
        args.Context?.Source?.TryGetTarget(out source);

        if (source != _cancelButton && _list.Value is int index)
        {
            Result = _versions[index];
        }

        return base.OnAccepting(args);
    }
}
