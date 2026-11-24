using Terminal.Gui.App;

namespace NugetScope.Tui.Threading;

/// <summary>
/// Text-only spinner for places a <c>SpinnerView</c> can't go (e.g. a border title).
/// Advances on a Terminal.Gui timer, so <see cref="Changed"/> is raised on the UI thread.
/// </summary>
internal sealed class TextSpinner(IApplication app) : IDisposable
{
    private static readonly string[] Frames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);

    private object? _timeout;
    private int _frameIndex;

    /// <summary>Raised on every frame and when the spinner starts or stops.</summary>
    public event EventHandler? Changed;

    public bool IsSpinning => _timeout is not null;

    public string Frame => Frames[_frameIndex % Frames.Length];

    public void Start()
    {
        if (_timeout is not null)
        {
            return;
        }

        _timeout = app.AddTimeout(Interval, () =>
        {
            _frameIndex++;
            Changed?.Invoke(this, EventArgs.Empty);
            return true; // Keep repeating until removed.
        });

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Stop()
    {
        if (_timeout is null)
        {
            return;
        }

        app.RemoveTimeout(_timeout);
        _timeout = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose() => Stop();
}
