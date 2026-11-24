using Terminal.Gui.App;

namespace NugetScope.Tui.Threading;

/// <summary>Progress reporter that always runs its handler on the UI thread.</summary>
internal sealed class UiProgress<T>(IApplication app, Action<T> handler) : IProgress<T>
{
    public void Report(T value) => app.Invoke(() => handler(value));
}
