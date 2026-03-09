// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Provides functionality to scan and identify orphaned generated files.
/// </summary>
public static class FileMetadataScanner
{
    /// <summary>
    /// Scans a directory for generated TypeScript files and identifies orphaned files.
    /// </summary>
    /// <param name="outputPath">The output directory to scan.</param>
    /// <param name="generatedFiles">Dictionary of currently generated files with their full paths.</param>
    /// <returns>Collection of orphaned file paths that should be removed.</returns>
    public static IEnumerable<string> FindOrphanedFiles(string outputPath, IDictionary<string, GeneratedFileMetadata> generatedFiles)
    {
        if (!Directory.Exists(outputPath))
        {
            return [];
        }

        var orphanedFiles = new List<string>();

        foreach (var filePath in Directory.GetFiles(outputPath, "*.ts", SearchOption.AllDirectories))
        {
            // Skip index files - they are handled separately
            if (Path.GetFileName(filePath) == "index.ts")
            {
                continue;
            }

            // Skip if this file was just generated
            if (generatedFiles.ContainsKey(filePath))
            {
                continue;
            }

            // Check if file has the generated marker
            if (GeneratedFileMetadata.IsGeneratedFile(filePath, out _))
            {
                // File has the marker but wasn't generated this time - it's orphaned
                orphanedFiles.Add(filePath);
            }
        }

        return orphanedFiles;
    }

    /// <summary>
    /// Finds files that are superseded by source file grouping.
    /// When <c>UseSourceFileAsOutputFile</c> is enabled, descriptors that were previously written to
    /// individual per-type files (e.g. <c>AllProspects.ts</c>) are now combined into a source-file-based
    /// output (e.g. <c>Listing.ts</c>). The old individual files become stale and should be removed,
    /// regardless of whether they carry the <c>@generated</c> marker.
    /// </summary>
    /// <param name="outputPath">The output directory.</param>
    /// <param name="segmentsToSkip">Number of namespace segments to skip when computing output paths.</param>
    /// <param name="allDescriptors">All descriptors that were generated in this run.</param>
    /// <param name="sourceFileMap">Map of type full name to source C# file name.</param>
    /// <param name="generatedFiles">Dictionary of currently generated files with their full paths.</param>
    /// <returns>Collection of superseded file paths that should be removed.</returns>
    public static IEnumerable<string> FindSupersededFiles(
        string outputPath,
        int segmentsToSkip,
        IEnumerable<IDescriptor> allDescriptors,
        IReadOnlyDictionary<string, string> sourceFileMap,
        IDictionary<string, GeneratedFileMetadata> generatedFiles)
    {
        var superseded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var descriptor in allDescriptors)
        {
            var typeName = descriptor.Type.FullName;
            if (typeName is null)
            {
                continue;
            }

            // Check if this type was redirected to a different file by the source file map
            if (!sourceFileMap.TryGetValue(typeName, out var sourceFileName))
            {
                continue;
            }

            // If the source file name matches the descriptor name, no redirect happened
            if (string.Equals(sourceFileName, descriptor.Name, StringComparison.Ordinal))
            {
                continue;
            }

            // Compute the old per-type file path that would have been generated without source file grouping
            var path = descriptor.Type.ResolveTargetPath(segmentsToSkip);
            var individualFilePath = Path.GetFullPath(Path.Join(outputPath, path, $"{descriptor.Name}.ts"));

            // If the individual file exists on disk and is not tracked as a current generated file, it's superseded
            if (File.Exists(individualFilePath) && !generatedFiles.ContainsKey(individualFilePath))
            {
                superseded.Add(individualFilePath);
            }
        }

        return superseded;
    }

    /// <summary>
    /// Removes orphaned files and cleans up empty directories.
    /// </summary>
    /// <param name="outputPath">The base output path.</param>
    /// <param name="orphanedFiles">Collection of orphaned file paths to remove.</param>
    /// <param name="message">Logger to use for outputting messages.</param>
    /// <returns>The number of files removed.</returns>
    public static int RemoveOrphanedFiles(string outputPath, IEnumerable<string> orphanedFiles, Action<string> message)
    {
        var orphanedFilesList = orphanedFiles.ToList();
        if (orphanedFilesList.Count == 0)
        {
            return 0;
        }

        message($"  Cleaning up {orphanedFilesList.Count} orphaned file(s)");

        var affectedDirectories = new HashSet<string>();

        foreach (var filePath in orphanedFilesList)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                var relativePath = Path.GetRelativePath(outputPath, filePath).Replace(Path.DirectorySeparatorChar, '/');
                message($"    Deleted orphaned: {relativePath}");

                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    affectedDirectories.Add(directory);
                }
            }
        }

        // Process affected directories from deepest to shallowest
        var sortedDirectories = affectedDirectories
            .OrderByDescending(d => d.Count(c => c == Path.DirectorySeparatorChar))
            .ToList();

        foreach (var directory in sortedDirectories)
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            CleanupDirectory(directory, outputPath, message);
        }

        return orphanedFilesList.Count;
    }

    static void CleanupDirectory(string directory, string outputPath, Action<string> message)
    {
        // Don't process directories outside the output path
        if (!directory.StartsWith(outputPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var files = Directory.GetFiles(directory, "*.ts")
            .Where(f => Path.GetFileName(f) != "index.ts")
            .ToList();

        var subdirectories = Directory.GetDirectories(directory);

        if (files.Count == 0 && subdirectories.Length == 0)
        {
            // Remove index.ts if it exists
            var indexPath = Path.Combine(directory, "index.ts");
            if (File.Exists(indexPath))
            {
                File.Delete(indexPath);
                var relativePath = Path.GetRelativePath(outputPath, indexPath).Replace(Path.DirectorySeparatorChar, '/');
                message($"    Deleted index: {relativePath}");
            }

            // Remove the empty directory
            Directory.Delete(directory);
            var relativeDir = Path.GetRelativePath(outputPath, directory).Replace(Path.DirectorySeparatorChar, '/');
            message($"    Removed empty directory: {relativeDir}");

            // Process parent directory
            var parent = Path.GetDirectoryName(directory);
            if (!string.IsNullOrEmpty(parent) && parent != outputPath)
            {
                CleanupDirectory(parent, outputPath, message);
            }
        }
    }
}
