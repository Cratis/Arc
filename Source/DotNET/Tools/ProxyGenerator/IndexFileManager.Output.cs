// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Output settings for generated index files.</summary>
public static partial class IndexFileManager
{
    /// <summary>
    /// Represents the file header and diagnostic output settings shared by index updates.
    /// </summary>
    /// <param name="FileHeaderTemplate">The file header template for a newly created index file.</param>
    /// <param name="Message">Logger to use for outputting messages.</param>
    /// <param name="OutputPath">The base output path for relative path calculation.</param>
    /// <param name="GeneratedOutput">Optional artifact output boundary.</param>
    sealed record IndexOutput(string FileHeaderTemplate, Action<string> Message, string OutputPath, IGeneratedOutput? GeneratedOutput = null)
    {
        /// <summary>Gets the artifact output boundary.</summary>
        public IGeneratedOutput Sink { get; } = GeneratedOutput ?? new FileSystemGeneratedOutput();

        /// <summary>Determines whether normal generation would retain the existing exports.</summary>
        /// <param name="location">The index and its generated files.</param>
        /// <param name="inDirectory">Files generated in this directory.</param>
        /// <param name="existing">The current exports.</param>
        /// <param name="expected">The expected exports.</param>
        /// <returns>Whether no rewrite is necessary.</returns>
        public bool CanKeepExports(IndexLocation location, DirectoryFiles inDirectory, List<string> existing, List<string> expected) =>
            !NeedsRewrite(inDirectory, existing.Exists(_ => IsStaleExport(_, location)), location) || existing.SequenceEqual(expected);

        /// <summary>Determines whether normal generation would retain equivalent content.</summary>
        /// <param name="path">The index path.</param>
        /// <param name="content">The rebuilt content.</param>
        /// <returns>Whether the existing content would be retained.</returns>
        public bool CanKeepContent(string path, string content) =>
            File.Exists(path) && Normalize(File.ReadAllText(path)) == Normalize(content);
    }
}
