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
    /// <param name="currentFiles">Collection of .ts files currently in the directory (excluding index.ts).</param>
    /// <param name="message">Logger to use for outputting messages.</param>
    /// <param name="outputPath">The base output path for relative path calculation.</param>
    public static void UpdateIndexFile(string directory, IEnumerable<string> currentFiles, Action<string> message, string outputPath)
    {
        var indexPath = Path.Combine(directory, "index.ts");
        var fileNames = currentFiles.Select(f => Path.GetFileNameWithoutExtension(f)).ToHashSet();

        // If there are no files, remove the index if it exists
        if (fileNames.Count == 0)
        {
            if (File.Exists(indexPath))
            {
                File.Delete(indexPath);
                message($"    Deleted empty index: {GetRelativePath(outputPath, indexPath)}");
            }
            return;
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
                    var exportPath = match.Groups[1].Value;
                    existingExports.Add(exportPath);
                }
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    // Keep non-export lines (comments, imports, etc.)
                    manualLines.Add(line);
                }
            }
        }

        // Build the new export list
        var newExports = new List<string>();

        // Add existing exports that are still valid
        foreach (var export in existingExports)
        {
            var fileName = export.TrimStart('.', '/');
            if (fileNames.Contains(fileName))
            {
                newExports.Add(export);
                fileNames.Remove(fileName);
            }
        }

        // Add new exports for files not in the existing index
        foreach (var fileName in fileNames.Order())
        {
            newExports.Add($"./{fileName}");
        }

        // Sort exports consistently
        newExports.Sort();

        // Build the final content
        var contentLines = new List<string>();

        // Add manual lines first (comments, imports, etc.)
        if (manualLines.Count > 0)
        {
            contentLines.AddRange(manualLines);
            if (newExports.Count > 0)
            {
                contentLines.Add(string.Empty); // Blank line before exports
            }
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

        File.WriteAllText(indexPath, content);
        var relPath = GetRelativePath(outputPath, indexPath);
        message($"    Updated index: {relPath}");
    }

    /// <summary>
    /// Updates all index.ts files in directories that have generated content.
    /// </summary>
    /// <param name="directories">Collection of directories with generated content.</param>
    /// <param name="message">Logger to use for outputting messages.</param>
    /// <param name="outputPath">The base output path.</param>
    public static void UpdateAllIndexFiles(IEnumerable<string> directories, Action<string> message, string outputPath)
    {
        foreach (var directory in directories)
        {
            var files = Directory.GetFiles(directory, "*.ts")
                .Where(f => Path.GetFileName(f) != "index.ts")
                .ToList();

            UpdateIndexFile(directory, files, message, outputPath);
        }
    }

    [GeneratedRegex(@"^\s*export\s+\*\s+from\s+['""](.+)['""]\s*;?\s*$", RegexOptions.ExplicitCapture)]
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
