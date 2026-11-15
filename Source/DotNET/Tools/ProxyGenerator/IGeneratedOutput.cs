// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Defines the mutation boundary for generated artifacts.
/// </summary>
internal interface IGeneratedOutput
{
    /// <summary>Gets whether complete artifact bytes must be compared.</summary>
    bool ComparesBytes { get; }

    /// <summary>Ensures the artifact directory exists.</summary>
    /// <param name="path">Directory path.</param>
    void EnsureDirectory(string path);

    /// <summary>Writes a complete artifact.</summary>
    /// <param name="path">Artifact path.</param>
    /// <param name="content">Complete artifact content.</param>
    /// <returns>Awaitable task.</returns>
    Task WriteFile(string path, string content);

    /// <summary>Deletes an obsolete artifact.</summary>
    /// <param name="path">Artifact path.</param>
    void DeleteFile(string path);

    /// <summary>Deletes an empty artifact directory.</summary>
    /// <param name="path">Directory path.</param>
    void DeleteDirectoryIfEmpty(string path);
}
