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
    /// <param name="message">Logger to use for outputting messages.</param>
    /// <param name="outputPath">The base output path for relative path calculation.</param>
    /// <returns>True if the index file was written, false if skipped.</returns>
    public static bool UpdateIndexFile(string directory, IDictionary<string, GeneratedFileMetadata> generatedFiles, Action<string> message, string outputPath)
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

        // Build the new export list
        var newExports = new List<string>();
        var manualExports = new List<string>();

        // Separate existing exports into managed (generated) and manual (non-generated)
        foreach (var export in existingExports)
        {
            var fileName = export.TrimStart('.', '/');
            if (fileNames.Contains(fileName))
            {
                // This is a managed export (corresponds to a generated file)
                newExports.Add(export);
                fileNames.Remove(fileName);
            }
            else
            {
                // This is a manual export (doesn't correspond to a generated file)
                manualExports.Add(export);
            }
        }

        // Track what managed exports existed before
        var existingManagedExports = newExports.ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Add new exports for files not in the existing index
        foreach (var fileName in fileNames.Order())
        {
            newExports.Add($"./{fileName}");
        }

        // Sort managed exports consistently
        newExports.Sort();

        // Check if managed exports changed
        var newManagedExports = newExports.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (existingManagedExports.SetEquals(newManagedExports))
        {
            // No changes to managed exports — nothing to change
            return false;
        }

        // Combine manual exports with managed exports
        var allExports = new List<string>();
        allExports.AddRange(manualExports);
        allExports.AddRange(newExports);
        allExports.Sort();
        newExports = allExports;

        // Build the final content
        var contentLines = new List<string>();

        // Add manual lines first (comments, imports, etc.)
        if (manualLines.Count > 0)
        {
            contentLines.AddRange(manualLines);
        }

        // Add export statements
        foreach (var export in newExports)
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
    /// <param name="message">Logger to use for outputting messages.</param>
    /// <param name="outputPath">The base output path.</param>
    /// <returns>A tuple containing (WrittenCount, SkippedCount).</returns>
    public static (int WrittenCount, int SkippedCount) UpdateAllIndexFiles(IEnumerable<string> directories, IDictionary<string, GeneratedFileMetadata> generatedFiles, Action<string> message, string outputPath)
    {
        var writtenCount = 0;
        var skippedCount = 0;

        foreach (var directory in directories)
        {
            var wasWritten = UpdateIndexFile(directory, generatedFiles, message, outputPath);
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
