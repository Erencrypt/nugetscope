namespace NugetScope.Tui.Formatting;

/// <summary>
/// Renders rows as aligned plain-text columns with a header rule. Columns are as wide as their longest value:
/// nothing is truncated, wide tables simply produce long lines (the detail pane scrolls horizontally).
/// </summary>
internal static class TextTable
{
    private const string Indent = "  ";
    private const string ColumnGap = "  ";

    public static IEnumerable<string> Render(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        int[] widths = headers
            .Select((header, column) => rows.Select(row => row[column].Length).Append(header.Length).Max())
            .ToArray();

        yield return FormatRow(headers, widths);
        yield return FormatRow(widths.Select(width => new string('─', width)).ToList(), widths);

        foreach (IReadOnlyList<string> row in rows)
        {
            yield return FormatRow(row, widths);
        }
    }

    private static string FormatRow(IReadOnlyList<string> cells, int[] widths) =>
        (Indent + string.Join(ColumnGap, cells.Select((cell, column) => cell.PadRight(widths[column])))).TrimEnd();
}
