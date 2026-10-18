// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Arc.Screenplay.Embedded.Build;

/// <summary>
/// Identifies everything the generated documents are a function of, so a build can tell that nothing they depend on changed.
/// </summary>
/// <remarks>
/// Generating the documents analyzes the whole application, which is the most expensive thing the build does for
/// them, and it was done on every build whether or not anything had changed. Files are identified the way
/// MSBuild's own up-to-date checks identify them - by path, size and last write time - because reading and hashing
/// every source and reference would cost a good part of what is being saved.
/// </remarks>
public class GenerationFingerprint
{
    readonly StringBuilder _content = new();

    /// <summary>
    /// Adds the settings the documents are generated with.
    /// </summary>
    /// <param name="name">The name of the setting.</param>
    /// <param name="value">The value of the setting.</param>
    /// <returns>The <see cref="GenerationFingerprint"/> for continuation.</returns>
    public GenerationFingerprint WithSetting(string name, string? value)
    {
        _content.Append("setting|").Append(name).Append('|').Append(value).Append('\n');

        return this;
    }

    /// <summary>
    /// Adds a set of files the documents are generated from.
    /// </summary>
    /// <param name="kind">What the files are, so the same file in two roles is not confused.</param>
    /// <param name="paths">The full paths of the files.</param>
    /// <returns>The <see cref="GenerationFingerprint"/> for continuation.</returns>
    public GenerationFingerprint WithFiles(string kind, IEnumerable<string> paths)
    {
        foreach (var path in paths.Where(_ => !string.IsNullOrWhiteSpace(_)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            _content.Append("file|").Append(kind).Append('|').Append(path).Append('|').Append(Describe(path)).Append('\n');
        }

        return this;
    }

    /// <summary>
    /// Adds the code that generates the documents, which is every assembly beside the one given.
    /// </summary>
    /// <param name="taskAssemblyPath">The path of the assembly holding the task.</param>
    /// <returns>The <see cref="GenerationFingerprint"/> for continuation.</returns>
    public GenerationFingerprint WithGeneratorAt(string taskAssemblyPath)
    {
        var directory = Path.GetDirectoryName(taskAssemblyPath);

        return directory is not null && Directory.Exists(directory)
            ? WithFiles("generator", Directory.EnumerateFiles(directory, "*.dll"))
            : WithSetting("generator", taskAssemblyPath);
    }

    /// <summary>
    /// Gets the fingerprint as a string.
    /// </summary>
    /// <returns>The hash of everything added.</returns>
    public override string ToString() =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(_content.ToString())));

    static string Describe(string path)
    {
        var file = new FileInfo(path);

        return file.Exists ? $"{file.Length}|{file.LastWriteTimeUtc.Ticks}" : "missing";
    }
}
