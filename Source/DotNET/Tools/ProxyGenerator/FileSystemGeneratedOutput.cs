// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Writes generated artifacts to the file system.
/// </summary>
internal sealed class FileSystemGeneratedOutput : IGeneratedOutput
{
    /// <inheritdoc/>
    public bool ComparesBytes => false;

    /// <inheritdoc/>
    public void EnsureDirectory(string path) => Directory.CreateDirectory(path);

    /// <inheritdoc/>
    public Task WriteFile(string path, string content) => File.WriteAllTextAsync(path, content);

    /// <inheritdoc/>
    public void DeleteFile(string path) => File.Delete(path);

    /// <inheritdoc/>
    public void DeleteDirectoryIfEmpty(string path) => Directory.Delete(path);
}
