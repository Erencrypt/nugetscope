namespace NugetScope.Core.Cli.ListPackage;

/// <summary>One <c>dotnet list package</c> variant. The status variants are mutually exclusive in the CLI.</summary>
internal enum ListPackageQuery
{
    Packages,
    Outdated,
    Deprecated,
    Vulnerable,
}
