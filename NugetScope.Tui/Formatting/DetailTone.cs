namespace NugetScope.Tui.Formatting;

/// <summary>Semantic colour of a text line or cell; mapped to actual colours by <see cref="Theming.Palette"/>.</summary>
internal enum DetailTone
{
    Normal,
    Heading,
    Muted,
    UpToDate,
    Vulnerable,
    Deprecated,
    Outdated,
    Mismatch,
    Unused,
    Error,
    Warning,
}
