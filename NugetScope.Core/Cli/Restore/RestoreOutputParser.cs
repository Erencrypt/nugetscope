using System.Text.RegularExpressions;
using NugetScope.Core.IO;
using NugetScope.Core.Packages;

namespace NugetScope.Core.Cli.Restore;

/// <summary>
/// Extracts MSBuild-style diagnostics from <c>dotnet restore</c> output, e.g.
/// <c>C:\repo\A\A.csproj : error NU1101: Unable to find package X. [C:\repo\Fx.slnx]</c>.
/// Restore has no JSON output, so this is the only way to tie a restore failure to a project.
/// </summary>
internal static partial class RestoreOutputParser
{
    public static IReadOnlyList<RestoreDiagnostic> Parse(string output)
    {
        var diagnostics = new List<RestoreDiagnostic>();

        foreach (string line in output.Split('\n'))
        {
            Match match = DiagnosticLine().Match(line.TrimEnd('\r'));
            if (!match.Success)
            {
                continue;
            }

            string file = match.Groups["file"].Value.Trim();
            var level = match.Groups["level"].Value.Equals("error", StringComparison.OrdinalIgnoreCase)
                ? ProblemLevel.Error
                : ProblemLevel.Warning;

            diagnostics.Add(new RestoreDiagnostic(
                file.Length == 0 ? null : PathNormalizer.Normalize(file),
                level,
                match.Groups["code"].Value,
                match.Groups["message"].Value.Trim()));
        }

        // MSBuild can repeat diagnostics in a summary block.
        return diagnostics.Distinct().ToList();
    }

    [GeneratedRegex(
        @"^\s*(?:(?<file>.+?)\s*:\s+)?(?<level>error|warning)\s+(?<code>[A-Z]+\d+)\s*:\s*(?<message>.*?)(?:\s+\[[^\]]+\])?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DiagnosticLine();
}
