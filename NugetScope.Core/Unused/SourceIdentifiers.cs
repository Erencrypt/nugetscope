using System.Text.RegularExpressions;

namespace NugetScope.Core.Unused;

/// <summary>
/// Every dotted name in some source text and all its prefixes: <c>using A.B.C;</c> yields A, A.B and A.B.C.
/// A package counts as used when one of its namespaces is in the set. Comments and strings are deliberately
/// included: a false "used" is harmless, a false "unused" is not.
/// </summary>
internal static partial class SourceIdentifiers
{
    public static void AddFrom(string text, ISet<string> identifiers)
    {
        foreach (Match match in DottedName().Matches(text))
        {
            string name = match.Value;
            for (int dot = name.IndexOf('.'); dot >= 0; dot = name.IndexOf('.', dot + 1))
            {
                identifiers.Add(name[..dot]);
            }

            identifiers.Add(name);
        }
    }

    [GeneratedRegex(@"@?[\p{L}_][\p{L}\p{Nd}_]*(?:\.@?[\p{L}_][\p{L}\p{Nd}_]*)*")]
    private static partial Regex DottedName();
}
