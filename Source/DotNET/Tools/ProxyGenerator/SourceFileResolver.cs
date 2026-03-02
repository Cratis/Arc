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

                foreach (var typeDefHandle in peMetadataReader.TypeDefinitions)
                {
                    var typeDef = peMetadataReader.GetTypeDefinition(typeDefHandle);
                    var typeNamespace = peMetadataReader.GetString(typeDef.Namespace);
                    var typeName = peMetadataReader.GetString(typeDef.Name);
                    var fullName = string.IsNullOrEmpty(typeNamespace) ? typeName : $"{typeNamespace}.{typeName}";

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
                            break;
                        }
                        catch
                        {
                            // Some methods may not have debug info
                        }
                    }
                }
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
}
