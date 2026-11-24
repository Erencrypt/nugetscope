namespace NugetScope.Core.Updates;

/// <summary>Lists the versions of a package available from the package sources configured for a directory.</summary>
public interface IPackageVersionSource
{
    /// <param name="workingDirectory">Directory whose nuget.config applies (the solution directory).</param>
    /// <exception cref="Packages.PackageSourceException">The search could not run or returned unreadable output.</exception>
    Task<PackageVersionList> GetVersionsAsync(string packageId, string workingDirectory, CancellationToken cancellationToken = default);
}
