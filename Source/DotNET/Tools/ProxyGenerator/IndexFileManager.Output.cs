// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

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
        public IGeneratedOutput Sink { get; } = GeneratedOutput ?? new FileSystemGeneratedOutput();

        public bool CanKeepExports(IndexLocation location, DirectoryFiles inDirectory, List<string> existing, List<string> expected) =>
            !Sink.ComparesBytes && (!NeedsRewrite(inDirectory, existing.Exists(_ => IsStaleExport(_, location)), location) || existing.SequenceEqual(expected));

        public bool CanKeepContent(string path, string content) =>
            !Sink.ComparesBytes && File.Exists(path) && Normalize(File.ReadAllText(path)) == Normalize(content);
    }
}
