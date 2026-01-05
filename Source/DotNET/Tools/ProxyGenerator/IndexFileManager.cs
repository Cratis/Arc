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

        // If no files in this directory were actually written, skip updating the index
        var anyWrittenInDirectory = generatedFiles.Any(kv => Path.GetDirectoryName(kv.Key) == directory && kv.Value.WasWritten);
        if (!anyWrittenInDirectory)
        {
            return false;
        }

        // Build set of orphaned file names in this directory
        var orphanedFileNames = orphanedFiles
            .Where(f => Path.GetDirectoryName(f) == directory)
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Build the final export list by processing existing exports
        var finalExports = new List<string>();
        var exportsToAdd = new HashSet<string>(fileNames, StringComparer.OrdinalIgnoreCase);

        foreach (var export in existingExports)
        {
            var fileName = export.TrimStart('.', '/');

            // If this export is for an orphaned file, remove it
            if (orphanedFileNames.Contains(fileName))
            {
                continue; // Skip orphaned exports
            }

            // Keep the export (whether it's managed or manual)
            finalExports.Add(export);

            // If it's a managed export, don't add it again later
            exportsToAdd.Remove(fileName);
        }

        // Add any new generated files that weren't in the index
        foreach (var fileName in exportsToAdd.Order())
        {
            finalExports.Add($"./{fileName}");
        }

        // Sort exports consistently
        finalExports.Sort();

        // Check if anything changed
        if (existingExports.Count == finalExports.Count &&
            existingExports.Order().SequenceEqual(finalExports.Order()))
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
