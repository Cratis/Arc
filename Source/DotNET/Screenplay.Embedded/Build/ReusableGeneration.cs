// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Arc.Screenplay.Embedded.Build;

/// <summary>
/// Remembers what generating the documents produced and what it was produced from, so an unchanged project can reuse it.
/// </summary>
/// <remarks>
/// Only a generation that reported nothing to look at is remembered. Anything that was worth a warning is worth
/// being shown again, so a build that produced one always runs again rather than staying quiet about it.
/// </remarks>
public static class ReusableGeneration
{
    /// <summary>
    /// The name of the file the fingerprint and the files it produced are remembered in.
    /// </summary>
    public const string FileName = "generation.fingerprint";

    /// <summary>
    /// Gets what a previous generation produced when it was produced from exactly the same inputs.
    /// </summary>
    /// <param name="directory">The directory the generated files are written to.</param>
    /// <param name="fingerprint">The fingerprint of the current inputs.</param>
    /// <param name="files">The files to embed, when they can be reused.</param>
    /// <returns>True when nothing changed and every file is still there, false otherwise.</returns>
    public static bool TryReuse(string directory, string fingerprint, out IReadOnlyList<EmbeddedResourceFile> files)
    {
        files = [];
        var path = Path.Join(directory, FileName);

        if (!File.Exists(path))
        {
            return false;
        }

        var lines = File.ReadAllLines(path);
        if (lines.Length < 2 || !string.Equals(lines[0], fingerprint, StringComparison.Ordinal))
        {
            return false;
        }

        var reused = new List<EmbeddedResourceFile>();
        foreach (var line in lines.Skip(1))
        {
            var separator = line.IndexOf('|', StringComparison.Ordinal);
            if (separator <= 0)
            {
                return false;
            }

            var file = new EmbeddedResourceFile(Path.Join(directory, Path.GetFileName(line[..separator])), line[(separator + 1)..]);
            if (!File.Exists(file.Path))
            {
                return false;
            }

            reused.Add(file);
        }

        files = reused;

        return true;
    }

    /// <summary>
    /// Remembers what was produced from the inputs with the given fingerprint.
    /// </summary>
    /// <param name="directory">The directory the generated files were written to.</param>
    /// <param name="fingerprint">The fingerprint of the inputs.</param>
    /// <param name="files">The files that were produced.</param>
    public static void Remember(string directory, string fingerprint, IEnumerable<EmbeddedResourceFile> files)
    {
        var content = new StringBuilder().Append(fingerprint).Append('\n');
        foreach (var file in files)
        {
            content.Append(Path.GetFileName(file.Path)).Append('|').Append(file.LogicalName).Append('\n');
        }

        File.WriteAllText(Path.Join(directory, FileName), content.ToString(), new UTF8Encoding(false));
    }

    /// <summary>
    /// Forgets what was remembered, so the next build generates again.
    /// </summary>
    /// <param name="directory">The directory the generated files are written to.</param>
    public static void Forget(string directory)
    {
        var path = Path.Join(directory, FileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
