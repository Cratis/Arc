// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Arc.Identity;

/// <summary>
/// Normalizes a percent-encoded request path so the path an on-behalf assertion is bound to (<c>htu</c>) can be compared
/// with the path the host received.
/// </summary>
/// <remarks>
/// AuthProxy and the host may percent-encode the same path differently - one escapes a character the other leaves
/// literal, or uses lowercase hex digits. Both sides are therefore decoded the way ASP.NET Core decodes a request path:
/// every escape is decoded except <c>%2F</c>, which stays encoded so <c>/a%2Fb</c> and <c>/a/b</c> remain different paths.
/// </remarks>
internal static class OnBehalfOfRequestPath
{
    /// <summary>
    /// Normalizes a percent-encoded path.
    /// </summary>
    /// <param name="path">The percent-encoded path.</param>
    /// <returns>The normalized path, or <see langword="null"/> when the path is not validly encoded UTF-8.</returns>
    internal static string? Normalize(string path)
    {
        var octets = new List<byte>(path.Length);
        for (var index = 0; index < path.Length; index++)
        {
            var character = path[index];
            if (character == '%' && index + 2 < path.Length && Uri.IsHexDigit(path[index + 1]) && Uri.IsHexDigit(path[index + 2]))
            {
                var octet = (byte)((Uri.FromHex(path[index + 1]) << 4) | Uri.FromHex(path[index + 2]));
                if (octet == (byte)'/')
                {
                    octets.AddRange("%2F"u8.ToArray());
                }
                else
                {
                    octets.Add(octet);
                }

                index += 2;
            }
            else
            {
                var length = char.IsHighSurrogate(character) && index + 1 < path.Length ? 2 : 1;
                octets.AddRange(Encoding.UTF8.GetBytes(path.Substring(index, length)));
                index += length - 1;
            }
        }

        try
        {
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString([.. octets]);
        }
        catch (DecoderFallbackException)
        {
            // Not valid UTF-8 once decoded: the path cannot match anything.
            return null;
        }
    }
}
