// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Arc.Identity;

/// <summary>
/// Strict unpadded base64url encoding, as used by JWS and by the on-behalf header hashes.
/// </summary>
internal static class OnBehalfOfBase64Url
{
    /// <summary>
    /// Decodes an unpadded base64url value, refusing padding and characters outside the base64url alphabet.
    /// </summary>
    /// <param name="value">The value to decode.</param>
    /// <param name="bytes">The decoded bytes.</param>
    /// <returns>True when the value decoded.</returns>
    internal static bool TryDecode(string value, out byte[] bytes)
    {
        bytes = [];
        if (value.Length == 0 || value.Length % 4 == 1 || !value.All(IsBase64UrlCharacter))
        {
            return false;
        }

        var base64 = new StringBuilder(value.Length + 3).Append(value).Replace('-', '+').Replace('_', '/');
        base64.Append('=', (4 - (value.Length % 4)) % 4);
        var buffer = new byte[base64.Length * 3 / 4];
        if (!Convert.TryFromBase64String(base64.ToString(), buffer, out var written))
        {
            return false;
        }

        bytes = buffer[..written];
        return true;
    }

    /// <summary>
    /// Computes the unpadded base64url SHA-256 of the ASCII bytes of a header value.
    /// </summary>
    /// <param name="headerValue">The exact header value.</param>
    /// <returns>The unpadded base64url digest.</returns>
    internal static string Sha256Of(string headerValue) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(headerValue)))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    static bool IsBase64UrlCharacter(char character) =>
        character is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-' or '_';
}
