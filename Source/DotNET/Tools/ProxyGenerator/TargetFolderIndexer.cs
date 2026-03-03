// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Indexes an existing target output folder to build a map of type full names to output file names.
/// </summary>
public static class TargetFolderIndexer
{
    /// <summary>
    /// Builds a map from type full name to output file name (without extension) by scanning existing generated TypeScript files.
    /// </summary>
    /// <param name="outputPath">Path to the output folder to scan.</param>
    /// <returns>A read-only dictionary mapping type full names to output file names.</returns>
    public static IReadOnlyDictionary<string, string> BuildTypeToOutputFileMap(string outputPath)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        if (!Directory.Exists(outputPath))
        {
            return result;
        }

        foreach (var filePath in Directory.GetFiles(outputPath, "*.ts", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(filePath) == "index.ts")
            {
                continue;
            }

            if (!GeneratedFileMetadata.IsGeneratedFile(filePath, out var metadata) || metadata is null)
            {
                continue;
            }

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

            foreach (var typeName in metadata.SourceTypeName.Split(", ", StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmedTypeName = typeName.Trim();
                if (!string.IsNullOrEmpty(trimmedTypeName))
                {
                    result[trimmedTypeName] = fileNameWithoutExtension;
                }
            }
        }

        return result;
    }
}
