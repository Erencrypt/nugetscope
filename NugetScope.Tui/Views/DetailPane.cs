using System.Collections.ObjectModel;
using NugetScope.Tui.Formatting;
using NugetScope.Tui.Theming;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace NugetScope.Tui.Views;

/// <summary>Framed, read-only, coloured text for the selected package. Scrolls both ways and never truncates.</summary>
/// <remarks>A ListView of lines: TextView is obsolete in Terminal.Gui 2.6 and Label does not scroll.</remarks>
internal sealed class DetailPane : FrameView
{
    private readonly ListView _list;
    private IReadOnlyList<DetailLine> _lines = [];

    public DetailPane()
    {
        Title = "Details";

        _list = new ListView
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };

        // ListView supports horizontal scrolling but leaves Left/Right unbound; wide table rows need it.
        _list.KeyBindings.Add(Key.CursorLeft, Command.ScrollLeft);
        _list.KeyBindings.Add(Key.CursorRight, Command.ScrollRight);
        _list.RowRender += OnRowRender;

        Add(_list);
    }

    public void ShowLines(IReadOnlyList<DetailLine> lines)
    {
        _lines = lines;
        _list.SetSource(new ObservableCollection<string>(lines.Select(l => l.Text)));
        _list.Value = null;
    }

    public void ShowText(string text) => ShowLines(DetailLine.FromText(text));

    private void OnRowRender(object? sender, ListViewRowEventArgs args)
    {
        // Leave the selected line alone so the theme's selection highlight stays visible.
        if (args.Row < 0 || args.Row >= _lines.Count || args.Row == _list.Value)
        {
            return;
        }

        DetailTone tone = _lines[args.Row].Tone;
        if (tone != DetailTone.Normal)
        {
            args.RowAttribute = Palette.Apply(tone, _list.GetAttributeForRole(VisualRole.Normal));
        }
    }
}
