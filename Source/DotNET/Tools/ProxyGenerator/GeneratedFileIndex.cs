// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Represents a hierarchical index of generated files.
/// </summary>
public class GeneratedFileIndex
{
    const string IndexFileName = "GeneratedFileIndex.json";
    const string CratisFolder = ".cratis";

    static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Gets the root entries of the file index.
    /// </summary>
    public IDictionary<string, FileIndexEntry> Entries { get; init; } = new Dictionary<string, FileIndexEntry>();

    /// <summary>
    /// Loads an existing file index from the specified project directory.
    /// </summary>
    /// <param name="projectDirectory">The project directory containing the .cratis folder.</param>
    /// <returns>The loaded file index, or an empty index if none exists.</returns>
    public static GeneratedFileIndex Load(string projectDirectory)
    {
        var indexPath = GetIndexPath(projectDirectory);
        if (!File.Exists(indexPath))
        {
            return new GeneratedFileIndex();
        }

        try
        {
            var json = File.ReadAllText(indexPath);
            return JsonSerializer.Deserialize<GeneratedFileIndex>(json, _jsonOptions) ?? new GeneratedFileIndex();
        }
        catch
        {
            return new GeneratedFileIndex();
        }
    }

    /// <summary>
    /// Saves the file index to the specified project directory.
    /// </summary>
    /// <param name="projectDirectory">The project directory where the .cratis folder should be created.</param>
    public void Save(string projectDirectory)
    {
        var cratisPath = Path.Combine(projectDirectory, CratisFolder);
        if (!Directory.Exists(cratisPath))
        {
            Directory.CreateDirectory(cratisPath);
        }

        var indexPath = GetIndexPath(projectDirectory);
        var json = JsonSerializer.Serialize(this, _jsonOptions);
        File.WriteAllText(indexPath, json);
    }

    /// <summary>
    /// Adds a generated file path to the index.
    /// </summary>
    /// <param name="relativePath">The relative path of the generated file from the output directory.</param>
    public void AddFile(string relativePath)
    {
        var normalizedPath = NormalizePath(relativePath);
        var parts = normalizedPath.Split('/');
        AddToEntries(Entries, parts, 0);
    }

    /// <summary>
    /// Gets all file paths in the index.
    /// </summary>
    /// <returns>A collection of relative file paths.</returns>
    public IEnumerable<string> GetAllFiles() => GetFilesRecursive(Entries, string.Empty);

    /// <summary>
    /// Gets the files that were removed compared to another index.
    /// </summary>
    /// <param name="previousIndex">The previous file index to compare against.</param>
    /// <returns>A collection of file paths that were present in the previous index but not in this one.</returns>
    public IEnumerable<string> GetRemovedFiles(GeneratedFileIndex previousIndex)
    {
        var currentFiles = new HashSet<string>(GetAllFiles());
        var previousFiles = previousIndex.GetAllFiles();

        return previousFiles.Where(f => !currentFiles.Contains(f));
    }

    static string GetIndexPath(string projectDirectory) =>
        Path.Combine(projectDirectory, CratisFolder, IndexFileName);

    static string NormalizePath(string path) =>
        path.Replace('\\', '/').TrimStart('/');

    static void AddToEntries(IDictionary<string, FileIndexEntry> entries, string[] parts, int index)
    {
        if (index >= parts.Length)
        {
            return;
        }

        var name = parts[index];
        var isFile = index == parts.Length - 1;

        if (isFile)
        {
            var folderName = parts.Length > 1 ? parts[index - 1] : string.Empty;
            if (string.IsNullOrEmpty(folderName))
            {
                return;
            }

            if (entries.TryGetValue(folderName, out var parentEntry))
            {
                parentEntry.Files ??= [];
                if (!parentEntry.Files.Contains(name))
                {
                    parentEntry.Files.Add(name);
                }
            }
        }
        else
        {
            if (!entries.TryGetValue(name, out var entry))
            {
                entry = new FileIndexEntry
                {
                    Folders = new Dictionary<string, FileIndexEntry>()
                };
                entries[name] = entry;
            }

            entry.Folders ??= new Dictionary<string, FileIndexEntry>();

            var nextIndex = index + 1;
            if (nextIndex < parts.Length)
            {
                var nextName = parts[nextIndex];
                var nextIsFile = nextIndex == parts.Length - 1;

                if (nextIsFile)
                {
                    entry.Files ??= [];
                    if (!entry.Files.Contains(nextName))
                    {
                        entry.Files.Add(nextName);
                    }
                }
                else
                {
                    AddToEntries(entry.Folders, parts, nextIndex);
                }
            }
        }
    }

    static IEnumerable<string> GetFilesRecursive(IDictionary<string, FileIndexEntry> entries, string basePath)
    {
        foreach (var (name, entry) in entries)
        {
            var currentPath = string.IsNullOrEmpty(basePath) ? name : $"{basePath}/{name}";

            if (entry.Files is not null)
            {
                foreach (var file in entry.Files)
                {
                    yield return $"{currentPath}/{file}";
                }
            }

            if (entry.Folders is not null)
            {
                foreach (var file in GetFilesRecursive(entry.Folders, currentPath))
                {
                    yield return file;
                }
            }
        }
    }
}
