namespace NugetScope.Core.Packages;

/// <summary>The package source failed as a whole (e.g. dotnet not installed, unreadable output). Per-project failures are reported as <see cref="PackageProblem"/> instead.</summary>
public sealed class PackageSourceException(string message, Exception? innerException = null)
    : Exception(message, innerException);
