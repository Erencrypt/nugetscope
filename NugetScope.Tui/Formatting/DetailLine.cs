namespace NugetScope.Tui.Formatting;

internal sealed record DetailLine(string Text, DetailTone Tone = DetailTone.Normal)
{
    public static IReadOnlyList<DetailLine> FromText(string text) =>
        text.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n').Select(line => new DetailLine(line)).ToList();
}
