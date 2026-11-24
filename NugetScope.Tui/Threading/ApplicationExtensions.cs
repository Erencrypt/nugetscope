using Terminal.Gui.App;

namespace NugetScope.Tui.Threading;

internal static class ApplicationExtensions
{
    /// <summary>Runs <paramref name="func"/> on the UI thread and completes with its result, so background flows read top to bottom.</summary>
    public static Task<T> InvokeAsync<T>(this IApplication app, Func<T> func)
    {
        // Continuations must not run inside the UI callback.
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        app.Invoke(() =>
        {
            try
            {
                completion.SetResult(func());
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        });

        return completion.Task;
    }

    public static Task InvokeAsync(this IApplication app, Action action) =>
        app.InvokeAsync(() =>
        {
            action();
            return true;
        });
}
