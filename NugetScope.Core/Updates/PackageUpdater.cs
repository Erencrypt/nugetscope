using NugetScope.Core.IO;

namespace NugetScope.Core.Updates;

/// <summary>Applies a confirmed <see cref="PackageUpdatePlan"/> by rewriting only the planned version values.</summary>
public static class PackageUpdater
{
    /// <summary>All edits are prepared first; if any file changed since the plan was made, nothing is written.</summary>
    /// <exception cref="PackageUpdateException">A file changed since planning, or could not be read or written.</exception>
    public static void Apply(PackageUpdatePlan plan)
    {
        var files = new List<TextFile>();

        foreach (var fileEdits in plan.Edits.GroupBy(e => e.Declaration.FilePath, PathNormalizer.Comparer))
        {
            TextFile file = ReadFile(fileEdits.Key);

            // Several kinds in one file (e.g. Version and VersionOverride) are located against the text as it is updated.
            foreach (PlannedEdit edit in fileEdits)
            {
                VersionLocation location = ProjectFileVersionFinder.Find(file.Text, edit.Declaration.Kind, plan.PackageId);

                if (location.Value != edit.OldValue)
                {
                    throw new PackageUpdateException(
                        $"{Path.GetFileName(file.Path)} changed since the preview ({edit.OldValue} is now {location.Value}). Nothing was written; reload and try again.");
                }

                file = file with { Text = location.ReplaceIn(file.Text, edit.NewValue) };
            }

            files.Add(file);
        }

        var written = new List<string>();
        foreach (TextFile file in files)
        {
            try
            {
                file.Write();
                written.Add(Path.GetFileName(file.Path));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                string already = written.Count > 0 ? $"Already written: {string.Join(", ", written)}." : "Nothing was written.";
                throw new PackageUpdateException($"Could not write {file.Path}: {ex.Message} {already}", ex);
            }
        }
    }

    private static TextFile ReadFile(string path)
    {
        try
        {
            return TextFile.Read(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new PackageUpdateException($"Could not read {path}: {ex.Message}", ex);
        }
    }
}
