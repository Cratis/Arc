// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Provides functionality to intelligently manage index.ts files.
/// </summary>
public static partial class IndexFileManager
{
    /// <summary>
    /// Updates or creates an index.ts file for a directory, preserving manual edits.
    /// </summary>
    /// <param name="directory">The directory to create/update the index.ts file in.</param>
    /// <param name="generatedFiles">Dictionary of generated file paths (full paths) to their metadata.</param>
    /// <param name="orphanedFiles">Collection of orphaned file paths that were deleted.</param>
    /// <param name="message">Logger to use for outputting messages.</param>
    /// <param name="outputPath">The base output path for relative path calculation.</param>
    /// <returns>True if the index file was written, false if skipped.</returns>
    public static bool UpdateIndexFile(string directory, IDictionary<string, GeneratedFileMetadata> generatedFiles, IEnumerable<string> orphanedFiles, Action<string> message, string outputPath)
    {
        var indexPath = Path.Combine(directory, "index.ts");

        // Filter to only files in this directory and extract their names
        var fileNames = generatedFiles.Keys
            .Where(f => Path.GetDirectoryName(f) == directory)
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // If there are no generated files in this directory, remove the index if it exists
        if (fileNames.Count == 0)
        {
            if (File.Exists(indexPath))
            {
                File.Delete(indexPath);
                message($"    Deleted empty index: {GetRelativePath(outputPath, indexPath)}");
            }
            return false;
        }

        var existingExports = new List<string>();
        var manualLines = new List<string>();

        // Parse existing index file if it exists
        if (File.Exists(indexPath))
        {
            foreach (var line in File.ReadAllLines(indexPath))
            {
                var match = ExportRegex().Match(line);
                if (match.Success)
                {
                    var exportPath = match.Groups["path"].Value;
                    existingExports.Add(exportPath);
                }
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    // Keep non-export lines (comments, imports, etc.)
                    manualLines.Add(line);
                }
            }
        }

        // Build set of orphaned file names in this directory
        var orphanedFileNames = orphanedFiles
            .Where(f => Path.GetDirectoryName(f) == directory)
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Check if we need to update: either new files were written OR orphans exist
        var anyWrittenInDirectory = generatedFiles.Any(kv => Path.GetDirectoryName(kv.Key) == directory && kv.Value.WasWritten);
        var hasOrphansInDirectory = orphanedFileNames.Count > 0;

        if (!anyWrittenInDirectory && !hasOrphansInDirectory)
        {
            return false;
        }

        // Build the final export list by inserting new exports at their alphabetically correct positions
        var finalExports = new List<string>();
        var existingFileNames = existingExports.Select(e => e.TrimStart('.', '/')).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newExportsToAdd = new List<string>();

        // Collect new exports to add (only those not already in the index)
        foreach (var fileName in fileNames)
        {
            if (!existingFileNames.Contains(fileName))
            {
                newExportsToAdd.Add(fileName);
            }
        }

        // Create a set to track which new exports have been inserted
        var insertedNewExports = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? previousFileName = null;

        // Process existing exports and insert new ones at their alphabetically correct positions
        foreach (var export in existingExports)
        {
            var fileName = export.TrimStart('.', '/');

            // If this export is for an orphaned file, remove it
            if (orphanedFileNames.Contains(fileName))
            {
                continue; // Skip orphaned exports
            }

            // Before adding this export, insert any NEW exports that fit between previous and current
            // They should be > previous (or no previous) AND < current
            var toInsertHere = newExportsToAdd
                .Where(newFile => !insertedNewExports.Contains(newFile) &&
                                 (previousFileName == null || string.Compare(newFile, previousFileName, StringComparison.OrdinalIgnoreCase) > 0) &&
                                 string.Compare(newFile, fileName, StringComparison.OrdinalIgnoreCase) < 0)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var newFile in toInsertHere)
            {
                finalExports.Add($"./{newFile}");
                insertedNewExports.Add(newFile);
            }

            // Keep the existing export
            finalExports.Add(export);
            previousFileName = fileName;
        }

        // Add any remaining new exports at the end (sorted alphabetically)
        // These are exports that are > all existing exports
        var remainingNewExports = newExportsToAdd
            .Where(newFile => !insertedNewExports.Contains(newFile))
            .Order(StringComparer.OrdinalIgnoreCase);

        foreach (var fileName in remainingNewExports)
        {
            finalExports.Add($"./{fileName}");
        }

        // Check if anything changed (compare unsorted since we preserve order)
        if (existingExports.Count == finalExports.Count &&
            existingExports.SequenceEqual(finalExports))
        {
            // No changes - don't rewrite
            return false;
        }

        // Build the final content
        var contentLines = new List<string>();

        // Add manual lines first (comments, imports, etc.)
        if (manualLines.Count > 0)
        {
            contentLines.AddRange(manualLines);
        }

        // Add export statements
        foreach (var export in finalExports)
        {
            contentLines.Add($"export * from '{export}';");
        }

        var content = string.Join(Environment.NewLine, contentLines);
        if (!string.IsNullOrEmpty(content))
        {
            content += Environment.NewLine;
        }

        // If the file exists, compare a normalized version (trim and ignore empty lines)
        if (File.Exists(indexPath))
        {
            var existing = File.ReadAllText(indexPath);
            static string Normalize(string s) => string.Join('\n', s.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()));
            if (Normalize(existing) == Normalize(content))
            {
                // Equivalent content (ignoring blank lines/trim) — don't rewrite
                return false;
            }
        }

        File.WriteAllText(indexPath, content);
        var relPath = GetRelativePath(outputPath, indexPath);
        message($"    Updated index: {relPath}");
        return true;
    }

    /// <summary>
    /// Updates all index.ts files in directories that have generated content.
    /// </summary>
    /// <param name="directories">Collection of directories with generated content.</param>
    /// <param name="generatedFiles">Collection of all generated file paths.</param>
    /// <param name="orphanedFiles">Collection of orphaned file paths that were deleted.</param>
    /// <param name="message">Logger to use for outputting messages.</param>
    /// <param name="outputPath">The base output path.</param>
    /// <returns>A tuple containing (WrittenCount, SkippedCount).</returns>
    public static (int WrittenCount, int SkippedCount) UpdateAllIndexFiles(IEnumerable<string> directories, IDictionary<string, GeneratedFileMetadata> generatedFiles, IEnumerable<string> orphanedFiles, Action<string> message, string outputPath)
    {
        var writtenCount = 0;
        var skippedCount = 0;

        foreach (var directory in directories)
        {
            var wasWritten = UpdateIndexFile(directory, generatedFiles, orphanedFiles, message, outputPath);
            if (wasWritten)
            {
                writtenCount++;
            }
            else
            {
                skippedCount++;
            }
        }

        return (writtenCount, skippedCount);
    }

    [GeneratedRegex(@"^\s*export\s+\*\s+from\s+['""](?<path>.+)['""]\s*;?\s*$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex ExportRegex();

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
