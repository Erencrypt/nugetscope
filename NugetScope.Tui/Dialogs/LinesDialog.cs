using NugetScope.Tui.Formatting;
using NugetScope.Tui.Views;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace NugetScope.Tui.Dialogs;

/// <summary>
/// Read-only coloured text in a dialog that scrolls both ways (help, problems). Unlike a MessageBox, long content
/// stays reachable and is never cut off.
/// </summary>
internal sealed class LinesDialog : Dialog
{
    public LinesDialog(string title, IReadOnlyList<DetailLine> lines)
    {
        Title = title;
        Width = Dim.Percent(80);
        Height = Dim.Percent(80);

        var pane = new DetailPane { Width = Dim.Fill(), Height = Dim.Fill(), BorderStyle = LineStyle.None };
        pane.ShowLines(lines);
        Add(pane);

        AddButton(new Button { Text = "_OK" });
        Initialized += (_, _) => pane.FocusList();
    }

    public static void Show(IApplication app, string title, IReadOnlyList<DetailLine> lines)
    {
        using var dialog = new LinesDialog(title, lines);
        app.Run(dialog);
    }
}
