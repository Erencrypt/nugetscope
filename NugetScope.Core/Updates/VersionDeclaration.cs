using NugetScope.Core.IO;

namespace NugetScope.Core.Updates;

/// <summary>The file and element that declare a package's version for a project.</summary>
public sealed record VersionDeclaration(string FilePath, VersionDeclarationKind Kind)
{
    public string FilePath { get; } = PathNormalizer.Normalize(FilePath);

    public bool Equals(VersionDeclaration? other) =>
        other is not null && Kind == other.Kind && PathNormalizer.Comparer.Equals(FilePath, other.FilePath);

    public override int GetHashCode() => HashCode.Combine(Kind, PathNormalizer.Comparer.GetHashCode(FilePath));
}
