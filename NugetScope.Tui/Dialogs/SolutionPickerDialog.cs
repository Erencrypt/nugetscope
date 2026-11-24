using System.Collections.ObjectModel;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace NugetScope.Tui.Dialogs;

/// <summary>Lets the user choose one of several solution files. Result is the full path, or null if cancelled.</summary>
internal sealed class SolutionPickerDialog : Dialog<string>
{
    private const int MaxVisibleItems = 15;

    private readonly IReadOnlyList<string> _paths;
    private readonly ListView _list;
    private readonly Button _cancelButton;

    public SolutionPickerDialog(IReadOnlyList<string> paths)
    {
        _paths = paths;
        Title = "Select a solution";

        var names = new ObservableCollection<string>(paths.Select(p => Path.GetFileName(p)));
        int widestName = names.Max(n => n.Length);

        _list = new ListView
        {
            Width = Dim.Fill(0, widestName + 2),
            Height = Math.Min(paths.Count, MaxVisibleItems),
        };
        _list.SetSource(names);
        _list.Value = 0;
        Add(_list);

        _cancelButton = new Button { Text = "_Cancel" };
        AddButton(_cancelButton);
        AddButton(new Button { Text = "_Open" }); // Last button added is the default.
    }

    // Dialog<T> stops on any Accept and treats a null Result as cancel. Enter in the list and the Open
    // button both arrive here; only the Cancel button must leave Result unset.
    protected override bool OnAccepting(CommandEventArgs args)
    {
        View? source = null;
        args.Context?.Source?.TryGetTarget(out source);

        if (source != _cancelButton && _list.Value is int index)
        {
            Result = _paths[index];
        }

        return base.OnAccepting(args);
    }
}
