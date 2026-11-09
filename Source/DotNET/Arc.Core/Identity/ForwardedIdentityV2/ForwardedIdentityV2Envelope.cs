// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>The strictly parsed v2 wire envelope; never log it.</summary>
/// <param name="Parts">The canonical wire components.</param>
/// <param name="KeyId">The signing key identifier.</param>
/// <param name="Timestamp">The Unix timestamp.</param>
/// <param name="Signature">The fixed-width P1363 signature.</param>
internal sealed partial record ForwardedIdentityV2Envelope(string[] Parts, string KeyId, long Timestamp, byte[] Signature)
{
    /// <summary>The envelope transport header.</summary>
    internal const string Header = ForwardedIdentityV2Headers.SignatureHeader;

    /// <summary>Parses the strict envelope grammar without accepting alternate encodings.</summary>
    /// <param name="text">The raw envelope value.</param>
    /// <returns>The parsed envelope or a fixed refusal reason.</returns>
    internal static (ForwardedIdentityV2Envelope? Envelope, ForwardedIdentityV2Rejection? Rejection) Parse(string text)
    {
        try
        {
            if (text.Length is 0 or > 256 || text.Any(c => c <= 32 || c >= 127)) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Format);
            var parts = text.Split('.');
            if (parts.Length != 5) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Format);
            if (parts[0] != "v2") throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Version);
            if (!KeyIdPattern().IsMatch(parts[1]) || !Unsigned(parts[2], out var timestamp)) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Format);
            _ = DecodeUrl(parts[3], 16);
            return (new(parts, parts[1], timestamp, DecodeUrl(parts[4], 64)), null);
        }
        catch (Exception error) when (ForwardedIdentityV2Rejected.Is(error, out var reason))
        {
            return (null, reason);
        }
    }

    /// <summary>Decodes one canonical, unpadded, fixed-size base64url value.</summary>
    /// <param name="value">The encoded value.</param>
    /// <param name="size">The required decoded byte count.</param>
    /// <returns>The decoded bytes.</returns>
    internal static byte[] DecodeUrl(string value, int size)
    {
        if (!Base64UrlPattern().IsMatch(value) || value.Length != ((size * 8) + 5) / 6) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Format);
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + new string('=', (4 - (value.Length % 4)) % 4));
        }
        catch (FormatException)
        {
            throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Format);
        }
        if (bytes.Length != size || Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_') != value) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Format);
        return bytes;
    }

    /// <summary>Parses canonical unsigned decimal text within Int64.</summary>
    /// <param name="value">The decimal text.</param>
    /// <param name="number">The parsed number.</param>
    /// <returns>Whether the text is canonical.</returns>
    internal static bool Unsigned(string value, out long number) => long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out number) && value.Length <= 19 && value == number.ToString(CultureInfo.InvariantCulture);

    /// <summary>Matches the bounded signing key identifier grammar.</summary>
    /// <returns>The key identifier matcher.</returns>
    [GeneratedRegex("\\A[A-Za-z0-9_-]{1,64}\\z", RegexOptions.None, 100)]
    internal static partial Regex KeyIdPattern();

    [GeneratedRegex("\\A[A-Za-z0-9_-]+\\z", RegexOptions.None, 100)]
    private static partial Regex Base64UrlPattern();
}
