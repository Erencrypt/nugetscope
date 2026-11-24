using NugetScope.Core.Packages;

namespace NugetScope.Tui.Formatting;

/// <summary>Building blocks shared by the detail formatters: heading, label/value fields, tables, notes.</summary>
internal static class DetailSections
{
    private const int LabelWidth = 12;

    public static void AddHeading(List<DetailLine> lines, string title)
    {
        lines.Add(new DetailLine(title, DetailTone.Heading));
        lines.Add(new DetailLine(new string('═', title.Length), DetailTone.Heading));
    }

    public static DetailLine Field(string label, string value, DetailTone tone = DetailTone.Normal) =>
        new(label.PadRight(LabelWidth) + value, tone);

    // TextTable yields the header, the rule, then one line per row, in that order.
    public static void AddTable(
        List<DetailLine> lines,
        string title,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows,
        Func<int, DetailTone> rowTone)
    {
        lines.Add(new DetailLine(""));
        lines.Add(new DetailLine(title, DetailTone.Heading));

        int index = 0;
        foreach (string line in TextTable.Render(headers, rows))
        {
            DetailTone tone = index < 2 ? DetailTone.Muted : rowTone(index - 2);
            lines.Add(new DetailLine(line, tone));
            index++;
        }
    }

    public static void AddUncheckedNote(List<DetailLine> lines, PackageChecks completedChecks)
    {
        PackageChecks missing = PackageChecks.All & ~completedChecks;
        if (missing != PackageChecks.None)
        {
            lines.Add(new DetailLine(""));
            lines.Add(new DetailLine($"Not checked: {missing.ToString().ToLowerInvariant()} (Ctrl+P shows why).", DetailTone.Muted));
        }
    }
}
