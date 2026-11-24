using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;

namespace NugetScope.Core.Solutions;

/// <summary>Reads .sln and .slnx files via Microsoft.VisualStudio.SolutionPersistence.</summary>
public sealed class SolutionReader : ISolutionReader
{
    // Project types that support PackageReference. Others (.sqlproj, .esproj, web sites) are skipped.
    private static readonly HashSet<string> SupportedProjectExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".csproj", ".fsproj", ".vbproj" };

    public async Task<SolutionInfo> ReadAsync(string solutionPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionPath);

        string fullPath = Path.GetFullPath(solutionPath);
        if (!File.Exists(fullPath))
        {
            throw new SolutionLoadException(fullPath, "Solution file not found.");
        }

        var serializer = SolutionSerializers.GetSerializerByMoniker(fullPath)
            ?? throw new SolutionLoadException(fullPath, "Unsupported solution format; expected .sln or .slnx.");

        SolutionModel model;
        try
        {
            model = await serializer.OpenAsync(fullPath, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new SolutionLoadException(fullPath, "Failed to parse solution file.", ex);
        }

        string solutionDirectory = Path.GetDirectoryName(fullPath)!;

        var projects = model.SolutionProjects
            .Where(p => SupportedProjectExtensions.Contains(Path.GetExtension(p.FilePath)))
            .Select(p => new ProjectInfo(
                p.ActualDisplayName,
                ResolveProjectPath(solutionDirectory, p.FilePath),
                ToFolderPath(p.Parent)))
            .ToList();

        return new SolutionInfo(fullPath, projects);
    }

    private static string ResolveProjectPath(string solutionDirectory, string projectPath)
    {
        // .sln files store Windows separators; normalize so paths resolve on any OS.
        string normalized = projectPath.Replace('\\', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(solutionDirectory, normalized));
    }

    private static string? ToFolderPath(SolutionFolderModel? folder)
    {
        if (folder is null)
        {
            return null;
        }

        // Folder.Path is like "/src/libs/".
        string trimmed = folder.Path.Trim('/');
        return trimmed.Length == 0 ? null : trimmed;
    }
}
