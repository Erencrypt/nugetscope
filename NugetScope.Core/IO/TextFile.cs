using System.Text;

namespace NugetScope.Core.IO;

/// <summary>A text file's content and encoding, so it can be written back byte-for-byte except for the edits (BOM included).</summary>
public sealed record TextFile(string Path, string Text, Encoding Encoding)
{
    public static TextFile Read(string path)
    {
        using var reader = new StreamReader(path, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), detectEncodingFromByteOrderMarks: true);
        string text = reader.ReadToEnd();
        return new TextFile(path, text, reader.CurrentEncoding);
    }

    public void Write() => File.WriteAllText(Path, Text, Encoding);
}
