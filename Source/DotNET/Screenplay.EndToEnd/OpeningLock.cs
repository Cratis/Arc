// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.EndToEnd;

/// <summary>
/// Holds a lock shared by every process of this tool on the machine while a workspace opens a project.
/// </summary>
/// <remarks>
/// Opening a project runs a design time build of it and of everything it references, and that build writes the
/// intermediate folder of each of them. Applications that reference the same projects can therefore not open at the
/// same moment without writing the same files, while everything after the open reads what the workspace holds in
/// memory. Holding this lock for the open alone is what lets several applications be generated at the same time
/// without any of them writing where another one does.
/// </remarks>
public sealed class OpeningLock : IDisposable
{
    static readonly string _path = Path.Combine(Path.GetTempPath(), "cratis-arc-screenplay-end-to-end-opening.lock");

    FileStream? _stream;

    OpeningLock(FileStream stream) => _stream = stream;

    /// <summary>
    /// Waits until no other process is opening a project, then holds the lock until it is disposed.
    /// </summary>
    /// <returns>The held lock.</returns>
    public static async Task<OpeningLock> Acquire()
    {
        while (true)
        {
            try
            {
                return new(new FileStream(_path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1));
            }
            catch (IOException)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250));
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _stream?.Dispose();
        _stream = null;
    }
}
