namespace NugetScope.Core.Updates;

/// <summary>A version value inside a file's text.</summary>
/// <param name="Start">Offset of the first character of the value.</param>
/// <param name="Value">The value exactly as written.</param>
public sealed record VersionLocation(int Start, string Value)
{
    public string ReplaceIn(string text, string newValue) =>
        string.Concat(text.AsSpan(0, Start), newValue, text.AsSpan(Start + Value.Length));
}
