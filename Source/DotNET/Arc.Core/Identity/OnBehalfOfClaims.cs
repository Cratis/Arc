// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Identity;

/// <summary>
/// Reads claims from the JSON header and payload of an on-behalf assertion.
/// </summary>
internal static class OnBehalfOfClaims
{
    /// <summary>
    /// Reads a string claim.
    /// </summary>
    /// <param name="element">The JSON object to read from.</param>
    /// <param name="name">The claim name.</param>
    /// <returns>The value, or <see langword="null"/> when the claim is missing or not a string.</returns>
    internal static string? String(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    /// <summary>
    /// Reads a NumericDate claim.
    /// </summary>
    /// <param name="element">The JSON object to read from.</param>
    /// <param name="name">The claim name.</param>
    /// <returns>The time, or <see langword="null"/> when the claim is missing or not a number of seconds.</returns>
    internal static DateTimeOffset? Time(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.Number &&
        value.TryGetDouble(out var seconds) &&
        seconds >= 0 &&
        seconds <= DateTimeOffset.MaxValue.ToUnixTimeSeconds()
            ? DateTimeOffset.FromUnixTimeSeconds((long)Math.Floor(seconds))
            : null;

    /// <summary>
    /// Checks whether the <c>aud</c> claim, a string or an array of strings, contains the audience.
    /// </summary>
    /// <param name="payload">The JSON payload.</param>
    /// <param name="audience">The expected audience.</param>
    /// <returns>True when the audience is present.</returns>
    internal static bool HasAudience(JsonElement payload, string audience)
    {
        if (!payload.TryGetProperty("aud", out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() == audience,
            JsonValueKind.Array => value.EnumerateArray().Any(_ => _.ValueKind == JsonValueKind.String && _.GetString() == audience),
            _ => false
        };
    }
}
