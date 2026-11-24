using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace NugetScope.Tui.Views;

internal sealed class MainWindow : Window
{
    public MainWindow()
    {
        Title = "nugetscope";

        Add(CreateMenuBar(), CreatePlaceholder(), CreateStatusBar());
    }

    private MenuBar CreateMenuBar() => new(
    [
        new MenuBarItem("_File",
        [
            new MenuItem("_Quit", "Exit nugetscope", RequestStop, Key.Q.WithCtrl),
        ]),
    ]);

    private static Label CreatePlaceholder() => new()
    {
        Text = "No solution loaded.",
        X = Pos.Center(),
        Y = Pos.Center(),
    };

    private StatusBar CreateStatusBar() => new(
    [
        new Shortcut(Key.Q.WithCtrl, "Quit", RequestStop, "Exit"),
    ]);
}
