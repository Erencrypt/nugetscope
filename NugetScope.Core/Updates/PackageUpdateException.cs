namespace NugetScope.Core.Updates;

/// <summary>An update could not be planned or applied. The message says whether any file was written.</summary>
public sealed class PackageUpdateException(string message, Exception? innerException = null)
    : Exception(message, innerException);
