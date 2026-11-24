using Terminal.Gui.Input;
using Terminal.Gui.Views;

namespace NugetScope.Tui.Export;

/// <summary>
/// The stock save dialog, with one change to what Save does. Highlighting a row copies its path into the path box,
/// and Save only reads that box, so stock behaviour is: a folder row makes Save do nothing (a folder isn't a .md
/// file) and a file row overwrites that file. Here a highlighted folder saves the report inside it, and a highlighted
/// file saves it in the shown folder; ".." stays stock and a file name the user typed is used as is.
/// </summary>
internal sealed class ReportSaveDialog : SaveDialog
{
    private string _fileName;
    private string? _typedPath;

    public ReportSaveDialog(string initialPath)
    {
        Path = initialPath;
        _fileName = System.IO.Path.GetFileName(initialPath);
        _typedPath = initialPath;

        // Enter on a file row, or in the path box, accepts without going through OnAccepting.
        FilesSelected += (_, _) => ResolvePath();
    }

    public override void EndInit()
    {
        base.EndInit();

        // Rows only change the box while the table has focus, so a change while the box has focus is typing.
        // Exact type: the file type drop-down derives from TextField.
        if (SubViews.FirstOrDefault(v => v.GetType() == typeof(TextField)) is TextField pathBox)
        {
            pathBox.TextChanged += (_, _) =>
            {
                if (pathBox.HasFocus)
                {
                    _typedPath = Path;
                    if (System.IO.Path.GetFileName(Path) is { Length: > 0 } name)
                    {
                        _fileName = name;
                    }
                }
            };
        }
    }

    // The Save button. Also runs for Cancel, which is harmless: the result is cleared either way.
    protected override bool OnAccepting(CommandEventArgs args)
    {
        ResolvePath();
        return base.OnAccepting(args);
    }

    private void ResolvePath()
    {
        if (Path == _typedPath)
        {
            return;
        }

        if (Directory.Exists(Path))
        {
            // A folder row. ".." (which puts the shown folder in the box) keeps the stock behaviour: nothing happens.
            if (!IsParentRowSelected())
            {
                Path = System.IO.Path.Combine(Path, _fileName);
            }
        }
        else if (File.Exists(Path) && System.IO.Path.GetDirectoryName(Path) is { } folder)
        {
            // A file row: never overwrite it by implication.
            Path = System.IO.Path.Combine(folder, _fileName);
        }
    }

    // The ".." row's first cell is exactly ".." (no icon); the table sits inside a container, so search the tree.
    private bool IsParentRowSelected() =>
        FindTable(this) is { Table: { } table, Value: { } selection }
        && selection.SelectedCell.Y >= 0 && selection.SelectedCell.Y < table.Rows
        && table[selection.SelectedCell.Y, 0] as string == "..";

    private static TableView? FindTable(Terminal.Gui.ViewBase.View view)
    {
        foreach (var child in view.SubViews)
        {
            if ((child as TableView ?? FindTable(child)) is { } found)
            {
                return found;
            }
        }

        return null;
    }
}
