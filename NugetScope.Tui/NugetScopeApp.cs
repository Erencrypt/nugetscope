using NugetScope.Core.Cli;
using NugetScope.Core.Scanning;
using NugetScope.Core.Solutions;
using NugetScope.Core.Updates;
using NugetScope.Tui.Dialogs;
using NugetScope.Tui.Updates;
using NugetScope.Tui.Views;
using Terminal.Gui.App;

namespace NugetScope.Tui;

/// <summary>Composition root: resolves the solution, wires services and runs the UI.</summary>
internal static class NugetScopeApp
{
    public static int Run(string[] args)
    {
        // Single optional positional argument for now; real option parsing comes with --json/CI mode.
        SolutionResolution resolution = SolutionLocator.Resolve(args.FirstOrDefault(), Environment.CurrentDirectory);

        if (resolution is SolutionResolution.NotFound notFound)
        {
            // Fail before taking over the terminal so the message stays visible.
            Console.Error.WriteLine($"nugetscope: {notFound.Message}");
            return 1;
        }

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
            return 0; // Picker cancelled.
        }

        var processRunner = new ProcessRunner();
        var scanner = new SolutionScanner(new SolutionReader(), new DotnetCliPackageSource(processRunner));
        var updateFlow = new PackageUpdateFlow(
            app,
            new DotnetCliPackageVersionSource(processRunner),
            new PackageUpdatePlanner(new DotnetCliProjectEvaluator(processRunner)));

        using var window = new MainWindow(app, scanner, updateFlow, solutionPath);
        app.Run(window);

        return 0;
    }

    private static string? PickSolution(IApplication app, IReadOnlyList<string> candidates)
    {
        using var picker = new SolutionPickerDialog(candidates);
        app.Run(picker);
        return picker.Result;
    }
}
