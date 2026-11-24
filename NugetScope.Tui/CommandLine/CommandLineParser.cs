using NugetScope.Core.Reporting;

namespace NugetScope.Tui.CommandLine;

/// <summary>Parses nugetscope's few options by hand (no dependency, AOT-friendly).</summary>
internal static class CommandLineParser
{
    public const string Usage = """
        Usage: nugetscope [solution] [options]

          solution              A .sln/.slnx file or a directory to search (default: current directory).

        Without options, opens the interactive dashboard. These options run without it (for CI):
          --json                Print a JSON report to stdout.
          --fail-on <statuses>  Exit with code 2 when a package has one of these statuses:
                                vulnerable, deprecated, outdated, mismatch (comma-separated) or any.
                                Prints a text report unless --json is given.
          --transitive          Include transitive packages (slower; checks them too).
          -h, --help            Show this help.

        Exit codes: 0 = OK, 1 = error or incomplete data (a check or project failed), 2 = --fail-on matched.
        """;

    /// <exception cref="ArgumentException">The arguments are invalid; the message says why.</exception>
    public static CommandLineOptions Parse(IReadOnlyList<string> args)
    {
        var options = new CommandLineOptions();

        for (int i = 0; i < args.Count; i++)
        {
            string arg = args[i];
            (string name, string? inlineValue) = SplitInlineValue(arg);

            switch (name)
            {
                case "-h" or "--help" or "-?":
                    options = options with { ShowHelp = true };
                    break;
                case "--json":
                    options = options with { Json = true };
                    break;
                case "--transitive":
                    options = options with { IncludeTransitive = true };
                    break;
                case "--fail-on":
                    string value = inlineValue ?? (i + 1 < args.Count ? args[++i] : throw new ArgumentException("--fail-on needs a value, e.g. --fail-on vulnerable."));
                    options = PackageStatusNames.TryParse(value, out var statuses)
                        ? options with { FailOn = options.FailOn | statuses }
                        : throw new ArgumentException($"Unknown --fail-on value '{value}'. Use vulnerable, deprecated, outdated, mismatch or any.");
                    break;
                case not null when arg.StartsWith('-'):
                    throw new ArgumentException($"Unknown option '{arg}'.");
                default:
                    options = options.SolutionArgument is null
                        ? options with { SolutionArgument = arg }
                        : throw new ArgumentException($"Only one solution can be given (got '{options.SolutionArgument}' and '{arg}').");
                    break;
            }
        }

        return options;
    }

    // Supports --fail-on=vulnerable as well as --fail-on vulnerable.
    private static (string Name, string? Value) SplitInlineValue(string arg)
    {
        int equals = arg.IndexOf('=');
        return arg.StartsWith("--") && equals > 0 ? (arg[..equals], arg[(equals + 1)..]) : (arg, null);
    }
}
