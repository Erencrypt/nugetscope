using NugetScope.Tui.Views;
using Terminal.Gui.App;

using IApplication app = Application.Create();
app.Init();

using MainWindow window = new();
app.Run(window);
