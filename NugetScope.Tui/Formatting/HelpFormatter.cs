using static NugetScope.Tui.Formatting.DetailSections;

namespace NugetScope.Tui.Formatting;

/// <summary>The F1 help: keys, navigation, and what each status (and its colour) means.</summary>
internal static class HelpFormatter
{
    private static readonly IReadOnlyList<IReadOnlyList<string>> Actions =
    [
        ["Enter", "Update the selected package (also Ctrl+U)"],
        ["F5", "Reload: restore and scan the solution again"],
        ["Ctrl+F", "Search this tab (package or project name)"],
        ["Ctrl+P", "Problems: restore and scan errors"],
        ["Ctrl+O", "Focus the details, or go back to the list"],
        ["Ctrl+G", "Group by project / by package"],
        ["Ctrl+T", "Show or hide transitive packages"],
        ["Ctrl+S", "Export a Markdown report"],        ["F3", "Next tab"],
        ["F1", "This help"],
        ["Ctrl+Q", "Quit"],
    ];

    private static readonly IReadOnlyList<IReadOnlyList<string>> Navigation =
    [
        ["↑ / ↓", "Move in the list; ↑ on the top row goes to search, ↑ in search to the tab header"],
        ["← / →", "Collapse / expand a project; switch tabs on a tab header; scroll the details sideways"],
        ["F6 / Shift+F6", "Move between the list and the details"],
        ["Tab / Shift+Tab", "Next / previous control"],
        ["F10", "Open the menu"],
    ];

    private static readonly IReadOnlyList<(string Status, string Meaning, DetailTone Tone)> Statuses =
    [
        ("vulnerable", "The version in use has a known security advisory", DetailTone.Vulnerable),
        ("deprecated", "The author marked the package deprecated", DetailTone.Deprecated),
        ("outdated", "A newer version is available", DetailTone.Outdated),
        ("mismatch", "Projects use different versions of the package", DetailTone.Mismatch),
        ("possibly unused", "No code in the project seems to use it (a hint: reflection or config-based use isn't detected)", DetailTone.Unused),
        ("up to date", "None of the above, and every check ran", DetailTone.UpToDate),
        (StatusFormatter.Unknown, "A check couldn't run (Ctrl+P shows why), so the status is unknown", DetailTone.Muted),
    ];

    public static IReadOnlyList<DetailLine> Format()
    {
        var lines = new List<DetailLine>();
        AddHeading(lines, "nugetscope");
        lines.Add(new DetailLine("NuGet packages across a whole solution. Updates always show the exact file changes first."));

        AddTable(lines, "Actions", ["Key", "Action"], Actions, _ => DetailTone.Normal);
        AddTable(lines, "Navigation", ["Key", "Action"], Navigation, _ => DetailTone.Normal);
        AddTable(
            lines,
            "Statuses",
            ["Status", "Meaning"],
            Statuses.Select(s => (IReadOnlyList<string>)[s.Status, s.Meaning]).ToList(),
            row => Statuses[row].Tone);

        return lines;
    }
}
