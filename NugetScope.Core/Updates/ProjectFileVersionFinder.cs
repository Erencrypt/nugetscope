using System.Xml;
using System.Xml.Linq;

namespace NugetScope.Core.Updates;

/// <summary>
/// Finds the exact text of a package version in an MSBuild file, as an attribute (<c>Version="1.0"</c>) or a child
/// element (<c>&lt;Version&gt;1.0&lt;/Version&gt;</c>). Edits replace only that value, so the rest of the file stays as written.
/// </summary>
public static class ProjectFileVersionFinder
{
    /// <exception cref="PackageUpdateException">The version is missing, ambiguous or not plain text in this file.</exception>
    public static VersionLocation Find(string text, VersionDeclarationKind kind, string packageId)
    {
        string elementName = kind == VersionDeclarationKind.PackageVersion ? "PackageVersion" : "PackageReference";
        string metadataName = kind == VersionDeclarationKind.VersionOverride ? "VersionOverride" : "Version";

        XDocument document;
        try
        {
            document = XDocument.Parse(text, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        }
        catch (XmlException ex)
        {
            throw new PackageUpdateException($"The file is not valid XML: {ex.Message}", ex);
        }

        var candidates = document.Descendants()
            .Where(e => e.Name.LocalName == elementName && DeclaresPackage(e, packageId))
            .Select(e => (Element: e, Attribute: FindAttribute(e, metadataName), Child: FindChild(e, metadataName)))
            .Where(c => c.Attribute is not null || c.Child is not null)
            .ToList();

        return candidates.Count switch
        {
            0 => throw new PackageUpdateException($"No {elementName} for {packageId} with a {metadataName} was found in this file."),
            > 1 => throw new PackageUpdateException($"{packageId} has {candidates.Count} {elementName} entries in this file (e.g. per condition)."),
            _ => candidates[0].Attribute is { } attribute
                ? LocateAttributeValue(text, attribute)
                : LocateElementValue(text, candidates[0].Child!),
        };
    }

    private static bool DeclaresPackage(XElement element, string packageId) =>
        string.Equals(
            (string?)FindAttribute(element, "Include") ?? (string?)FindAttribute(element, "Update"),
            packageId,
            StringComparison.OrdinalIgnoreCase);

    // MSBuild metadata names are case-insensitive.
    private static XAttribute? FindAttribute(XElement element, string name) =>
        element.Attributes().FirstOrDefault(a => a.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase));

    private static XElement? FindChild(XElement element, string name) =>
        element.Elements().FirstOrDefault(e => e.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase));

    // Line info points at the attribute name; the value is the text between the quotes after '='.
    private static VersionLocation LocateAttributeValue(string text, XAttribute attribute)
    {
        int nameStart = Offset(text, attribute);
        int equals = text.IndexOf('=', nameStart);
        int quote = equals < 0 ? -1 : text.IndexOfAny(['"', '\''], equals);

        if (!text.AsSpan(nameStart).StartsWith(attribute.Name.LocalName) || quote < 0)
        {
            throw new PackageUpdateException("The version attribute could not be located in the file text.");
        }

        int start = quote + 1;
        int end = text.IndexOf(text[quote], start);

        return Checked(text, start, end, attribute.Value);
    }

    // Line info points at the element name; the value is the text between '>' and the closing tag.
    private static VersionLocation LocateElementValue(string text, XElement element)
    {
        if (element.HasElements || element.IsEmpty)
        {
            throw new PackageUpdateException($"The <{element.Name.LocalName}> element does not hold a plain version.");
        }

        int nameStart = Offset(text, element);
        int start = text.IndexOf('>', nameStart) + 1;
        int end = start <= 0 ? -1 : text.IndexOf('<', start);

        if (end < 0)
        {
            throw new PackageUpdateException("The version element could not be located in the file text.");
        }

        // Leave surrounding whitespace (e.g. a value on its own line) in place.
        string raw = text[start..end];
        start += raw.Length - raw.TrimStart().Length;
        end -= raw.Length - raw.TrimEnd().Length;

        return Checked(text, start, end, element.Value.Trim());
    }

    // Guards against entities or anything else that makes the raw text differ from the parsed value.
    private static VersionLocation Checked(string text, int start, int end, string parsedValue)
    {
        if (end < start || text[start..end] != parsedValue)
        {
            throw new PackageUpdateException("The version is written in a form that can't be edited safely (e.g. XML entities).");
        }

        return new VersionLocation(start, parsedValue);
    }

    private static int Offset(string text, IXmlLineInfo lineInfo)
    {
        int offset = 0;
        for (int line = 1; line < lineInfo.LineNumber; line++)
        {
            offset = text.IndexOf('\n', offset) + 1;
        }

        return offset + lineInfo.LinePosition - 1;
    }
}
