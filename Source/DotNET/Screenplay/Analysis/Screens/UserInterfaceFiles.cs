// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Represents an implementation of <see cref="IUserInterfaceFiles"/> reading the file system the source was built from.
/// </summary>
/// <remarks>
/// This is the only place in analysis that touches a disk, which is the point of it being behind an interface. A
/// directory a compilation names may not exist by the time a document is generated from it - a path from another
/// machine, a source generator, or a compilation built entirely in memory - and none of those is an error, so an
/// unreadable directory simply holds nothing.
/// </remarks>
public class UserInterfaceFiles : IUserInterfaceFiles
{
    /// <inheritdoc/>
    public IEnumerable<string> In(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(directory, $"*{ScreenFiles.Extension}", SearchOption.TopDirectoryOnly)
            .Where(ScreenFiles.IsUserInterfaceFile);
    }

    /// <inheritdoc/>
    public IEnumerable<string> Under(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory) || !IsSourceDirectory(directory))
        {
            return [];
        }

        return Descend(directory);
    }

    /// <inheritdoc/>
    public string? Contents(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        try
        {
            return File.ReadAllText(path);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    static IEnumerable<string> Descend(string directory) => Directory.EnumerateFiles(directory, "*.tsx")
        .Where(file => !Path.GetFileName(file).StartsWith('.') && !File.GetAttributes(file).HasFlag(FileAttributes.ReparsePoint))
        .Concat(Directory.EnumerateDirectories(directory).Where(IsSourceDirectory).SelectMany(Descend));

    static bool IsSourceDirectory(string directory)
    {
        var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(directory));
        return (name == ".frontend" || !name.StartsWith('.')) &&
            name is not ("node_modules" or "bin" or "obj" or "dist" or "wwwroot") &&
            !File.GetAttributes(directory).HasFlag(FileAttributes.ReparsePoint);
    }
}
