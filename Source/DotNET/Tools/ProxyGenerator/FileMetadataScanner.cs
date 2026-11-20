// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    /// Removes orphaned files and cleans up empty directories.
    /// </summary>
    /// <param name="outputPath">The base output path.</param>
    /// <param name="orphanedFiles">Collection of orphaned file paths to remove.</param>
    /// <param name="message">Logger to use for outputting messages.</param>
    /// <returns>The number of files removed.</returns>
    public static int RemoveOrphanedFiles(string outputPath, IEnumerable<string> orphanedFiles, Action<string> message)
        => RemoveOrphanedFiles(orphanedFiles, new GeneratedOutputScope(outputPath, message, new FileSystemGeneratedOutput()));

    /// <summary>Finds orphaned proxies owned by the previous generation.</summary>
    /// <param name="outputPath">The output root.</param>
    /// <param name="generatedFiles">The files generated in this run.</param>
    /// <param name="scope">The output settings and recorded ownership.</param>
    /// <returns>The previously produced proxies that are now orphaned.</returns>
    internal static List<string> FindOrphanedFiles(string outputPath, IDictionary<string, GeneratedFileMetadata> generatedFiles, GeneratedOutputScope scope) =>
        scope.PreviousFiles?.Count == 0 ? [] : FindOrphanedFiles(outputPath, generatedFiles).Where(scope.Owns).ToList();

    /// <summary>Removes orphaned artifacts through the selected output boundary.</summary>
    /// <param name="orphanedFiles">The orphaned artifact paths.</param>
    /// <param name="scope">The output settings and recorded ownership.</param>
    /// <returns>The number of orphaned artifacts.</returns>
    internal static int RemoveOrphanedFiles(IEnumerable<string> orphanedFiles, GeneratedOutputScope scope)
    {
        var orphanedFilesList = orphanedFiles.ToList();
        if (orphanedFilesList.Count == 0)
        {
            return 0;
        }

        scope.Message($"  Cleaning up {orphanedFilesList.Count} orphaned file(s)");

        var affectedDirectories = new HashSet<string>();
        foreach (var filePath in orphanedFilesList.Where(File.Exists))
        {
            DeleteOrphanedFile(filePath, scope, affectedDirectories);
        }

        CleanupDirectories(affectedDirectories, scope);
        return orphanedFilesList.Count;
    }

    static void DeleteOrphanedFile(string filePath, GeneratedOutputScope scope, HashSet<string> affectedDirectories)
    {
        scope.Output.DeleteFile(filePath);
        scope.Message($"    Deleted orphaned: {scope.RelativePath(filePath)}");
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory)) affectedDirectories.Add(directory);
    }

    static void CleanupDirectories(IEnumerable<string> directories, GeneratedOutputScope scope)
    {
        if (scope.Output.ComparesBytes) return;

        // Process affected directories from deepest to shallowest
        var sortedDirectories = directories.OrderByDescending(d => d.Count(c => c == Path.DirectorySeparatorChar)).ToList();
        foreach (var directory in sortedDirectories.Where(Directory.Exists))
        {
            CleanupDirectory(directory, scope);
        }
    }

    static void CleanupDirectory(string directory, GeneratedOutputScope scope)
    {
        // Don't process directories outside the output path
        if (!directory.StartsWith(scope.OutputPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (HasTypeScriptFiles(directory) || Directory.GetDirectories(directory).Length != 0) return;
        if (!RemoveOwnedIndex(directory, scope)) return;

        // Remove the empty directory
        scope.Output.DeleteDirectoryIfEmpty(directory);
        scope.Message($"    Removed empty directory: {scope.RelativePath(directory)}");

        // Process parent directory
        var parent = Path.GetDirectoryName(directory);
        if (!string.IsNullOrEmpty(parent) && parent != scope.OutputPath)
        {
            CleanupDirectory(parent, scope);
        }
    }

    static bool HasTypeScriptFiles(string directory) => Directory.GetFiles(directory).Any(file =>
    {
        var extension = Path.GetExtension(file);
        return (string.Equals(extension, ".ts", StringComparison.Ordinal) || string.Equals(extension, ".tsx", StringComparison.Ordinal)) && Path.GetFileName(file) != "index.ts";
    });

    static bool RemoveOwnedIndex(string directory, GeneratedOutputScope scope)
    {
        var indexPath = Path.Combine(directory, "index.ts");
        if (!File.Exists(indexPath)) return true;
        if (!scope.Owns(indexPath)) return false;
        scope.Output.DeleteFile(indexPath);
        scope.Message($"    Deleted index: {scope.RelativePath(indexPath)}");
        return true;
    }
}
