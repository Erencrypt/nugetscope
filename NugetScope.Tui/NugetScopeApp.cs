using NugetScope.Core.Cli;
using NugetScope.Core.Scanning;
using NugetScope.Core.Solutions;
using NugetScope.Core.Updates;
using NugetScope.Tui.CommandLine;
using NugetScope.Tui.Dialogs;
using NugetScope.Tui.Updates;
using NugetScope.Tui.Views;
using Terminal.Gui.App;

namespace NugetScope.Tui;

/// <summary>Composition root: parses arguments, resolves the solution, then runs the dashboard or the headless (CI) mode.</summary>
internal static class NugetScopeApp
{
    public static int Run(string[] args)
    {
        CommandLineOptions options;
        try
        {
            options = CommandLineParser.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"nugetscope: {ex.Message}\n\n{CommandLineParser.Usage}");
            return ExitCodes.Error;
        }

        if (options.ShowHelp)
        {
            Console.WriteLine(CommandLineParser.Usage);
            return ExitCodes.Success;
        }

        SolutionResolution resolution = SolutionLocator.Resolve(options.SolutionArgument, Environment.CurrentDirectory);

        if (resolution is SolutionResolution.NotFound notFound)
        {
            // Fail before taking over the terminal so the message stays visible.
            Console.Error.WriteLine($"nugetscope: {notFound.Message}");
            return ExitCodes.Error;
        }

        var processRunner = new ProcessRunner();
        var scanner = new SolutionScanner(new SolutionReader(), new DotnetCliPackageSource(processRunner));

        if (options.Headless)
        {
            return RunHeadless(scanner, resolution, options);
        }

        try
        {
            return RunDashboard(scanner, processRunner, resolution);
        }
        catch (Exception ex)
        {
            // The application is disposed by now, so the terminal is restored and the message stays readable.
            Console.Error.WriteLine($"nugetscope: unexpected error. Please report it with the details below.\n\n{ex}");
            return ExitCodes.Error;
        }
    }

    private static int RunHeadless(SolutionScanner scanner, SolutionResolution resolution, CommandLineOptions options)
    {
        if (resolution is SolutionResolution.Ambiguous ambiguous)
        {
            // No picker without the dashboard: the caller has to name the solution.
            Console.Error.WriteLine($"nugetscope: several solutions found; pass one of them:\n  {string.Join("\n  ", ambiguous.Candidates)}");
            return ExitCodes.Error;
        }

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true; // Let the scan stop its dotnet processes instead of killing nugetscope outright.
            cancellation.Cancel();
        };

        try
        {
            string solutionPath = ((SolutionResolution.Found)resolution).SolutionPath;
            return HeadlessRunner.RunAsync(scanner, solutionPath, options, cancellation.Token).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("nugetscope: cancelled.");
            return ExitCodes.Error;
        }
    }

    private static int RunDashboard(SolutionScanner scanner, IProcessRunner processRunner, SolutionResolution resolution)
    {
        using IApplication app = Application.Create();
        app.Init();

        string? solutionPath = resolution switch
        {
            SolutionResolution.Found found => found.SolutionPath,
            SolutionResolution.Ambiguous ambiguous => PickSolution(app, ambiguous.Candidates),
            _ => null,
        };

        if (solutionPath is null)
        {
            return ExitCodes.Success; // Picker cancelled.
        }

        var updateFlow = new PackageUpdateFlow(
            app,
            new DotnetCliPackageVersionSource(processRunner),
            new PackageUpdatePlanner(new DotnetCliProjectEvaluator(processRunner)));

        using var window = new MainWindow(app, scanner, updateFlow, solutionPath);
        app.Run(window);

        return ExitCodes.Success;
    }

    private static string? PickSolution(IApplication app, IReadOnlyList<string> candidates)
    {
        using var picker = new SolutionPickerDialog(candidates);
        app.Run(picker);
        return picker.Result;
    }
}
