// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Arc.Identity;

/// <summary>
/// Represents a strictly parsed v1 signature envelope.
/// </summary>
/// <param name="KeyId">The case-sensitive key identifier.</param>
/// <param name="TimestampText">The canonical decimal timestamp.</param>
/// <param name="Timestamp">The Unix seconds timestamp.</param>
/// <param name="Nonce">The canonical base64url nonce.</param>
/// <param name="Mac">The decoded 32-byte MAC.</param>
internal sealed record ForwardedIdentityEnvelope(string KeyId, string TimestampText, long Timestamp, string Nonce, byte[] Mac)
{
    /// <summary>
    /// The v1 signature header name.
    /// </summary>
    internal const string Header = "x-cratis-forwarded-identity-signature";

    /// <summary>
    /// Validates a protocol key identifier.
    /// </summary>
    /// <param name="value">The identifier.</param>
    /// <returns>Whether the identifier is valid.</returns>
    internal static bool IsKeyId(string? value) => value is { Length: >= 1 and <= 64 } && value.All(character =>
        character is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_' or '-');

    /// <summary>
    /// Parses a bounded canonical envelope without decoding any principal data.
    /// </summary>
    /// <param name="value">The sole raw signature value.</param>
    /// <returns>The envelope, or null if invalid.</returns>
    internal static ForwardedIdentityEnvelope? Parse(string? value)
    {
        if (value is not { Length: >= 1 and <= 256 } || value.Any(character => character > 127))
        {
            return null;
        }
        var parts = value.Split('.');
        if (parts.Length != 5 || parts[0] != "v1" || !IsKeyId(parts[1]) ||
            parts[2].Length is < 1 or > 19 || (parts[2].Length > 1 && parts[2][0] == '0') ||
            parts[2].Any(character => character is < '0' or > '9') ||
            !long.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var timestamp))
        {
            return null;
        }
        var nonce = Decode(parts[3]);
        var mac = Decode(parts[4]);
        return nonce is null || mac is null ? null : new(parts[1], parts[2], timestamp, parts[3], mac);
    }

    static byte[]? Decode(string value)
    {
        if (value.Length != 43 || value.Any(character => character is not ((>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_' or '-')))
        {
            return null;
        }
        var bytes = Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + "=");
        var canonical = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return bytes.Length == 32 && canonical == value ? bytes : null;
    }
}
