using System.Collections.ObjectModel;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace NugetScope.Tui.Views;

/// <summary>Framed, read-only, scrollable text for details of the current selection.</summary>
/// <remarks>Uses a ListView of lines: TextView is obsolete in Terminal.Gui 2.6 and Label does not scroll.</remarks>
internal sealed class DetailPane : FrameView
{
    private readonly ListView _lines;

    public DetailPane()
    {
        Title = "Details";

        _lines = new ListView
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };

        Add(_lines);
    }

    public void ShowText(string text)
    {
        string[] lines = text.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n');
        _lines.SetSource(new ObservableCollection<string>(lines));
        _lines.Value = null;
    }
}
