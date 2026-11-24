using System.Text;
using NugetScope.Core.Reporting;
using NugetScope.Tui.State;
using Terminal.Gui.App;
using Terminal.Gui.Views;

namespace NugetScope.Tui.Export;

/// <summary>
/// Saves what the dashboard shows as a Markdown report: the stock save dialog picks the file, and an existing
/// file is only replaced after confirming.
/// </summary>
internal static class MarkdownExport
{
    public static void Run(IApplication app, DashboardState state, string solutionPath)
    {
        if (state.Scan is not { } scan)
        {
            MessageBox.Query(app, "Export", "Nothing to export yet. Wait for the scan to finish.", "_OK");
            return;
        }

        string? path = PickFile(app, solutionPath);
        if (path is null)
        {
            return;
        }

        // Cancel is last, so it is the default button: Enter alone never overwrites a file.
        if (File.Exists(path) && MessageBox.Query(app, "Export", $"{path} already exists. Replace it?", "_Replace", "_Cancel") != 0)
        {
            return;
        }

        try
        {
            // The unused check may still be running; the report then says it wasn't checked.
            File.WriteAllText(path, MarkdownReportWriter.Write(scan, state.Packages, state.Unused, evaluation: null), new UTF8Encoding(false));
            MessageBox.Query(app, "Export", $"Report saved to {path}", "_OK");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.ErrorQuery(app, "Export failed", ex.Message, "_OK");
        }
    }

    private static string? PickFile(IApplication app, string solutionPath)
    {
        string directory = Path.GetDirectoryName(Path.GetFullPath(solutionPath))!;
        string fileName = $"{Path.GetFileNameWithoutExtension(solutionPath)}-packages.md";

        using var dialog = new ReportSaveDialog(Path.Combine(directory, fileName))
        {
            Title = "Export Markdown report",
            AllowedTypes = [new AllowedType("Markdown", ".md")],
        };
        app.Run(dialog);

        return dialog.Canceled || string.IsNullOrWhiteSpace(dialog.Path) ? null : Path.GetFullPath(dialog.Path);
    }
}
