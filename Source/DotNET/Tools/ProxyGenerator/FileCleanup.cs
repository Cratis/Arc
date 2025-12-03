// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Provides functionality to clean up removed generated files and update index.ts files.
/// </summary>
public static class FileCleanup
{
    /// <summary>
    /// Removes files that were previously generated but are no longer present.
    /// Updates index.ts files and removes empty directories.
    /// </summary>
    /// <param name="outputPath">The base output path for generated files.</param>
    /// <param name="removedFiles">The collection of relative file paths that were removed.</param>
    /// <param name="message">Logger to use for outputting messages.</param>
    public static void RemoveFiles(string outputPath, IEnumerable<string> removedFiles, Action<string> message)
    {
        var removedFilesList = removedFiles.ToList();
        if (removedFilesList.Count == 0)
        {
            return;
        }

        message($"  Cleaning up {removedFilesList.Count} removed file(s)");

        var affectedDirectories = new HashSet<string>();

        foreach (var relativePath in removedFilesList)
        {
            var fullPath = Path.Combine(outputPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                message($"    Deleted: {relativePath}");

                var directory = Path.GetDirectoryName(fullPath);
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

            ProcessDirectory(directory, outputPath, message);
        }
    }

    static void ProcessDirectory(string directory, string outputPath, Action<string> message)
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
                message($"    Deleted index: {GetRelativePath(outputPath, indexPath)}");
            }

            // Remove the empty directory
            Directory.Delete(directory);
            message($"    Removed empty directory: {GetRelativePath(outputPath, directory)}");

            // Process parent directory
            var parent = Path.GetDirectoryName(directory);
            if (!string.IsNullOrEmpty(parent) && parent != outputPath)
            {
                ProcessDirectory(parent, outputPath, message);
            }
        }
        else if (files.Count > 0)
        {
            // Update index.ts to reflect current files
            UpdateIndexFile(directory, files, message, outputPath);
        }
    }

    static void UpdateIndexFile(string directory, List<string> files, Action<string> message, string outputPath)
    {
        var indexPath = Path.Combine(directory, "index.ts");

        var exports = files
            .Select(f => $"./{Path.GetFileNameWithoutExtension(f)}")
            .OrderBy(Path.GetFileName)
            .ToList();

        var content = string.Join(Environment.NewLine, exports.Select(e => $"export * from '{e}';")) + Environment.NewLine;

        File.WriteAllText(indexPath, content);
        message($"    Updated index: {GetRelativePath(outputPath, indexPath)}");
    }

    static string GetRelativePath(string basePath, string fullPath)
    {
        if (fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
        {
            var relative = fullPath[basePath.Length..].TrimStart(Path.DirectorySeparatorChar);

            return relative.Replace(Path.DirectorySeparatorChar, '/');
        }

        return fullPath;
    }
}
