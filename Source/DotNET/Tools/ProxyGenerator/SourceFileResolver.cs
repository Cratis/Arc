// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Resolves the source C# file name for types using PDB debug information.
/// </summary>
public static class SourceFileResolver
{
    /// <summary>
    /// Builds a map from type full name to the source C# file name (without extension).
    /// </summary>
    /// <param name="assemblyPath">Path to the assembly file.</param>
    /// <returns>A read-only dictionary mapping type full names to source file names.</returns>
    public static IReadOnlyDictionary<string, string> BuildTypeToSourceFileMap(string assemblyPath)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        if (!File.Exists(assemblyPath))
        {
            return result;
        }

        try
        {
            using var assemblyStream = File.OpenRead(assemblyPath);
            using var peReader = new PEReader(assemblyStream);

            MetadataReaderProvider? debugProvider = null;
            try
            {
                var debugDirectories = peReader.ReadDebugDirectory();
                var embeddedPdbEntry = debugDirectories.FirstOrDefault(d => d.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);

                if (embeddedPdbEntry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb)
                {
                    debugProvider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(embeddedPdbEntry);
                }
                else
                {
                    var pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
                    if (File.Exists(pdbPath))
                    {
                        var pdbBytes = File.ReadAllBytes(pdbPath);
                        debugProvider = MetadataReaderProvider.FromPortablePdbImage(
                            System.Collections.Immutable.ImmutableArray.Create(pdbBytes));
                    }
                }

                if (debugProvider is null)
                {
                    return result;
                }

                var debugReader = debugProvider.GetMetadataReader();
                var peMetadataReader = peReader.GetMetadataReader();

                // Track types without methods (e.g. enums) for a second-pass resolution.
                var unmappedTypes = new List<(string FullName, string Namespace)>();

                foreach (var typeDefHandle in peMetadataReader.TypeDefinitions)
                {
                    var typeDef = peMetadataReader.GetTypeDefinition(typeDefHandle);
                    var typeNamespace = peMetadataReader.GetString(typeDef.Namespace);
                    var typeName = peMetadataReader.GetString(typeDef.Name);
                    var fullName = string.IsNullOrEmpty(typeNamespace) ? typeName : $"{typeNamespace}.{typeName}";
                    var resolved = false;

                    foreach (var methodHandle in typeDef.GetMethods())
                    {
                        try
                        {
                            var rowNumber = MetadataTokens.GetRowNumber(methodHandle);
                            var debugInfoHandle = MetadataTokens.MethodDebugInformationHandle(rowNumber);
                            var methodDebugInfo = debugReader.GetMethodDebugInformation(debugInfoHandle);

                            if (methodDebugInfo.Document.IsNil)
                            {
                                continue;
                            }

                            var document = debugReader.GetDocument(methodDebugInfo.Document);
                            var documentName = debugReader.GetString(document.Name);
                            result[fullName] = Path.GetFileNameWithoutExtension(documentName);
                            resolved = true;
                            break;
                        }
                        catch
                        {
                            // Some methods may not have debug info
                        }
                    }

                    if (!resolved && !string.IsNullOrEmpty(typeNamespace) && !typeName.StartsWith('<'))
                    {
                        unmappedTypes.Add((fullName, typeNamespace));
                    }
                }

                // Second pass: resolve unmapped types (enums, empty records, etc.) by finding
                // sibling types in the same namespace that all map to the same source file.
                ResolveUnmappedTypes(result, unmappedTypes);
            }
            finally
            {
                debugProvider?.Dispose();
            }
        }
        catch
        {
            // PDB reading failed - return empty result
        }

        return result;
    }

    /// <summary>
    /// Resolves unmapped types (such as enums that have no methods with debug info) by finding
    /// sibling types in the same namespace that were successfully resolved to a source file.
    /// When all resolved siblings in a namespace point to the same source file, the unmapped
    /// type is assigned that source file.
    /// </summary>
    /// <param name="result">The map to update with resolved types.</param>
    /// <param name="unmappedTypes">The types that could not be resolved from method debug info.</param>
    public static void ResolveUnmappedTypes(
        IDictionary<string, string> result,
        IReadOnlyList<(string FullName, string Namespace)> unmappedTypes)
    {
        if (unmappedTypes.Count == 0)
        {
            return;
        }

        // Group already-resolved types by namespace and their source files.
        var namespaceToSourceFiles = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var (fullName, sourceFile) in result)
        {
            var dotIndex = fullName.LastIndexOf('.');
            if (dotIndex <= 0)
            {
                continue;
            }

            var ns = fullName[..dotIndex];
            if (!namespaceToSourceFiles.TryGetValue(ns, out var files))
            {
                files = new HashSet<string>(StringComparer.Ordinal);
                namespaceToSourceFiles[ns] = files;
            }

            files.Add(sourceFile);
        }

        foreach (var (fullName, ns) in unmappedTypes)
        {
            if (result.ContainsKey(fullName))
            {
                continue;
            }

            if (namespaceToSourceFiles.TryGetValue(ns, out var sourceFiles) && sourceFiles.Count == 1)
            {
                result[fullName] = sourceFiles.First();
            }
        }
    }
}
