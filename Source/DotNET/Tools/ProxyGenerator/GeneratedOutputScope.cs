// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Groups the output boundary, diagnostics, and ownership of a generation run.</summary>
/// <param name="OutputPath">The root output directory.</param>
/// <param name="Message">The progress logger.</param>
/// <param name="Output">The artifact output boundary.</param>
/// <param name="PreviousFiles">Previously owned paths, or null for unscoped operations.</param>
internal sealed record GeneratedOutputScope(string OutputPath, Action<string> Message, IGeneratedOutput Output, IReadOnlyCollection<string>? PreviousFiles = null)
{
    /// <summary>Determines whether cleanup is allowed to remove a path.</summary>
    /// <param name="path">The artifact path.</param>
    /// <returns>Whether the path belongs to this scope.</returns>
    internal bool Owns(string path) => PreviousFiles?.Contains(Path.GetFullPath(path), StringComparer.Ordinal) != false;

    /// <summary>Formats a path for progress messages.</summary>
    /// <param name="path">The artifact path.</param>
    /// <returns>The path relative to the output root, with forward slashes.</returns>
    internal string RelativePath(string path) => Path.GetRelativePath(OutputPath, path).Replace(Path.DirectorySeparatorChar, '/');
}
