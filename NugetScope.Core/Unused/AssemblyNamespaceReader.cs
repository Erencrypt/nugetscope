using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace NugetScope.Core.Unused;

/// <summary>
/// Namespaces of an assembly's public types (and types it forwards), read from metadata only: nothing is loaded
/// or executed. Results are cached per file, since many projects share the same packages.
/// </summary>
internal sealed class AssemblyNamespaceReader
{
    private readonly ConcurrentDictionary<string, IReadOnlySet<string>> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Empty when the file can't be read as an assembly.</summary>
    public IReadOnlySet<string> Read(string assemblyPath) => _cache.GetOrAdd(assemblyPath, ReadUncached);

    private static IReadOnlySet<string> ReadUncached(string assemblyPath)
    {
        try
        {
            using FileStream stream = File.OpenRead(assemblyPath);
            using var pe = new PEReader(stream);

            if (!pe.HasMetadata)
            {
                return new HashSet<string>();
            }

            MetadataReader metadata = pe.GetMetadataReader();

            var publicTypes = metadata.TypeDefinitions
                .Select(metadata.GetTypeDefinition)
                .Where(type => (type.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public)
                .Select(type => metadata.GetString(type.Namespace));

            // Facade assemblies only forward types elsewhere; the forwarded namespaces still count.
            var forwardedTypes = metadata.ExportedTypes
                .Select(metadata.GetExportedType)
                .Where(type => type.IsForwarder)
                .Select(type => metadata.GetString(type.Namespace));

            return publicTypes.Concat(forwardedTypes).Where(ns => ns.Length > 0).ToHashSet(StringComparer.Ordinal);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or BadImageFormatException)
        {
            return new HashSet<string>();
        }
    }
}
