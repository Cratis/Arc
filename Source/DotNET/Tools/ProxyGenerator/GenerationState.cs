// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Records the inputs and outputs of a successful proxy generation.
/// </summary>
/// <param name="Fingerprint">The generation input fingerprint.</param>
/// <param name="Outputs">The generated output metadata.</param>
internal sealed record GenerationState(string Fingerprint, GenerationState.Output[] Outputs)
{
    /// <summary>
    /// Checks whether the recorded inputs and outputs still match.
    /// </summary>
    /// <param name="stateFile">The saved state file.</param>
    /// <param name="fingerprint">The current input fingerprint.</param>
    /// <returns>True when generation can be skipped.</returns>
    internal static bool IsUpToDate(string stateFile, string fingerprint)
    {
        try
        {
            using var stream = File.OpenRead(stateFile);
            var state = JsonSerializer.Deserialize<GenerationState>(stream);
            return state?.Fingerprint == fingerprint &&
                state?.Outputs?.All(output => output?.Matches() == true) == true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Atomically saves the successful generation state.
    /// </summary>
    /// <param name="stateFile">The destination state file.</param>
    /// <param name="fingerprint">The generation input fingerprint.</param>
    /// <param name="producedFiles">The generated output paths.</param>
    internal static void Save(string stateFile, string fingerprint, IEnumerable<string> producedFiles)
    {
        stateFile = Path.GetFullPath(stateFile);
        Directory.CreateDirectory(Path.GetDirectoryName(stateFile)!);
        var outputs = producedFiles.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)
            .Select(path => new FileInfo(path))
            .Select(file => new Output(file.FullName, file.Length, file.LastWriteTimeUtc, GetContentHash(file.FullName)))
            .ToArray();
        var temporaryFile = $"{stateFile}.{Guid.NewGuid():N}.tmp";
        try
        {
            using (var stream = new FileStream(temporaryFile, FileMode.CreateNew, FileAccess.Write))
            {
                JsonSerializer.Serialize(stream, new GenerationState(fingerprint, outputs));
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporaryFile, stateFile, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryFile);
        }
    }

    static string GetContentHash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    /// <summary>
    /// Records the metadata and content hash of a generated file.
    /// </summary>
    /// <param name="Path">The absolute output path.</param>
    /// <param name="Length">The output length in bytes.</param>
    /// <param name="LastWriteUtc">The output modification time.</param>
    /// <param name="ContentHash">The SHA-256 hash of the output content.</param>
    internal sealed record Output(string Path, long Length, DateTime LastWriteUtc, string ContentHash)
    {
        /// <summary>
        /// Checks whether the output still matches its recorded state.
        /// </summary>
        /// <returns>True when metadata and content match.</returns>
        internal bool Matches()
        {
            var file = new FileInfo(Path);
            return file.Exists && file.Length == Length && file.LastWriteTimeUtc == LastWriteUtc &&
                GetContentHash(file.FullName) == ContentHash;
        }
    }
}
