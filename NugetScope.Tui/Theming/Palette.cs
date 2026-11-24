using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Tui.Formatting;
using Terminal.Gui.Drawing;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace NugetScope.Tui.Theming;

/// <summary>Status colours. Only foregrounds change; backgrounds come from the active theme.</summary>
internal static class Palette
{
    /// <summary>The most severe issue decides the colour: vulnerable, then deprecated, outdated, mismatch.</summary>
    public static DetailTone ToneFor(PackageStatus status, PackageChecks completedChecks) => status switch
    {
        _ when status.HasFlag(PackageStatus.Vulnerable) => DetailTone.Vulnerable,
        _ when status.HasFlag(PackageStatus.Deprecated) => DetailTone.Deprecated,
        _ when status.HasFlag(PackageStatus.Outdated) => DetailTone.Outdated,
        _ when status.HasFlag(PackageStatus.Mismatch) => DetailTone.Mismatch,
        _ when completedChecks == PackageChecks.All => DetailTone.UpToDate,
        _ => DetailTone.Muted,
    };

    public static DetailTone ToneFor(PackageUsage usage) => usage switch
    {
        { IsVulnerable: true } => DetailTone.Vulnerable,
        { IsDeprecated: true } => DetailTone.Deprecated,
        { IsOutdated: true } => DetailTone.Outdated,
        _ => DetailTone.Normal,
    };

    /// <summary>Foreground for a tone, or null to keep the theme's default.</summary>
    public static ColorName16? Foreground(DetailTone tone) => tone switch
    {
        DetailTone.Heading => ColorName16.BrightCyan,
        DetailTone.Muted => ColorName16.DarkGray,
        DetailTone.UpToDate => ColorName16.BrightGreen,
        DetailTone.Vulnerable or DetailTone.Error => ColorName16.BrightRed,
        DetailTone.Deprecated => ColorName16.BrightMagenta,
        DetailTone.Outdated or DetailTone.Warning => ColorName16.BrightYellow,
        DetailTone.Mismatch => ColorName16.BrightBlue,
        _ => null,
    };

    /// <summary>The theme attribute with its foreground replaced by the tone's colour.</summary>
    public static Attribute Apply(DetailTone tone, Attribute themeAttribute) =>
        Foreground(tone) is { } foreground ? new Attribute(foreground, themeAttribute.Background) : themeAttribute;
}
