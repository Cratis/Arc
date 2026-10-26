// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Arc.Identity;

/// <summary>
/// Decodes an identity header value that AuthProxy may carry in RFC 8187 <c>ext-value</c> form.
/// </summary>
/// <remarks>
/// A header field cannot carry characters above <c>U+007F</c>, so AuthProxy sends an identity value such as
/// <see cref="MicrosoftIdentityPlatformHeaders.IdentityIdHeader"/> as <c>UTF-8''</c> followed by percent-encoded UTF-8
/// when it is not printable US-ASCII, or when it already starts with <c>UTF-8''</c>. Every other value travels verbatim.
/// Decoding exactly the values carrying the prefix is therefore the inverse of what AuthProxy does, for every value.
/// </remarks>
internal static class ForwardedHeaderValue
{
    /// <summary>
    /// The RFC 8187 charset-and-language prefix an encoded value carries.
    /// </summary>
    internal const string ExtendedValuePrefix = "UTF-8''";

    /// <summary>
    /// Decodes a header value.
    /// </summary>
    /// <param name="value">The value as it arrived on the header.</param>
    /// <returns>The original value, or <see langword="null"/> when the value carries the prefix but is not validly encoded.</returns>
    internal static string? Decode(string value)
    {
        if (!value.StartsWith(ExtendedValuePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        var encoded = value.AsSpan(ExtendedValuePrefix.Length);
        var octets = new byte[encoded.Length];
        var count = 0;
        for (var index = 0; index < encoded.Length; index++)
        {
            var character = encoded[index];
            if (character == '%')
            {
                if (index + 2 >= encoded.Length ||
                    !Uri.IsHexDigit(encoded[index + 1]) ||
                    !Uri.IsHexDigit(encoded[index + 2]))
                {
                    return null;
                }

                octets[count++] = (byte)((Uri.FromHex(encoded[index + 1]) << 4) | Uri.FromHex(encoded[index + 2]));
                index += 2;
            }
            else if (character < 0x80)
            {
                octets[count++] = (byte)character;
            }
            else
            {
                return null;
            }
        }

        try
        {
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(octets, 0, count);
        }
        catch (DecoderFallbackException)
        {
            // Announced as encoded but not valid UTF-8: there is no original value to recover.
            return null;
        }
    }
}
