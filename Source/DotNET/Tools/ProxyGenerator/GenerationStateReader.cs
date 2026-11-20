// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Reads artifact ownership from a previous generation's saved state.</summary>
internal static class GenerationStateReader
{
    /// <summary>Reads the output paths owned by the previous successful generation.</summary>
    /// <param name="stateFile">The saved state file, if configured.</param>
    /// <returns>The recorded output paths, or none when state is unavailable.</returns>
    internal static IReadOnlyCollection<string> ReadProducedFiles(string? stateFile)
    {
        if (string.IsNullOrEmpty(stateFile)) return [];
        try
        {
            using var stream = File.OpenRead(stateFile);
            var state = JsonSerializer.Deserialize<GenerationState>(stream);
            if (state?.Outputs is not { } outputs || outputs.Any(output => string.IsNullOrEmpty(output?.Path))) return [];
            return outputs.Select(output => Path.GetFullPath(output.Path)).ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            return [];
        }
    }
}
