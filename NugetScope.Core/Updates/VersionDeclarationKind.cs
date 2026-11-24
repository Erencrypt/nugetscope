namespace NugetScope.Core.Updates;

/// <summary>Where a package's version is written.</summary>
public enum VersionDeclarationKind
{
    /// <summary><c>Version</c> on a project's <c>PackageReference</c> (no Central Package Management).</summary>
    PackageReference,

    /// <summary><c>VersionOverride</c> on a <c>PackageReference</c> under Central Package Management.</summary>
    VersionOverride,

    /// <summary><c>Version</c> on a central <c>PackageVersion</c> (usually in <c>Directory.Packages.props</c>).</summary>
    PackageVersion,
}
