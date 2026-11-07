// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>A canonical v1 PAT, not an authenticated credential. Never log its explicitly revealed value or secret.</summary>
internal sealed class PatFormat
{
    readonly byte[] _secret;

    PatFormat(PatResourceKey resourceKey, byte[] secret)
    {
        ResourceKey = resourceKey;
        _secret = secret;
    }

    /// <summary>Gets the canonical resource key, not proof of resource binding.</summary>
    internal PatResourceKey ResourceKey { get; }

    /// <summary>Gets the secret for local verifier derivation. Never log this value.</summary>
    internal ReadOnlySpan<byte> Secret => _secret;

    /// <inheritdoc/>
    public override string ToString() => nameof(PatFormat);

    /// <summary>Creates a new random payload, not a stored or authorized credential.</summary>
    /// <param name="resourceKey">The canonical resource key.</param>
    /// <returns>The generated format value.</returns>
    internal static PatFormat Create(PatResourceKey resourceKey) => new(resourceKey, RandomNumberGenerator.GetBytes(32));

    /// <summary>Checks canonical encoding and checksum only; never authenticates a credential.</summary>
    /// <param name="value">The exact credential value, without normalization.</param>
    /// <param name="parsed">The parsed value, or null on any format refusal.</param>
    /// <returns>Whether the canonical format and transcription checksum are valid.</returns>
    internal static bool TryParse(string? value, [NotNullWhen(true)] out PatFormat? parsed)
    {
        parsed = null;
        if (value is null || value.Length is < 87 or > 135) return false;
        var segments = value.Split('_');
        if (!HasCanonicalSegments(segments)) return false;
        if (!IsLowerHex(segments[3], 64) || !IsLowerHex(segments[4], 8)) return false;

        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(value[..^9]));
        var checksum = Convert.FromHexString(segments[4]);
        if (!CryptographicOperations.FixedTimeEquals(hash.AsSpan(0, 4), checksum)) return false;
        parsed = new(new PatResourceKey(segments[0]), Convert.FromHexString(segments[3]));
        return true;
    }

    /// <summary>Explicitly reveals the bearer value. Never use in logs, exceptions, or telemetry.</summary>
    /// <returns>The complete credential value.</returns>
    internal string Reveal()
    {
        var body = $"{ResourceKey.Value}_pat_v1_{Convert.ToHexString(_secret).ToLowerInvariant()}";
        var checksum = Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(body))).ToLowerInvariant()[..8];
        return $"{body}_{checksum}";
    }

    static bool HasCanonicalSegments(string[] segments)
    {
        if (segments.Length != 5) return false;
        return PatResourceKey.IsValid(segments[0]) && segments[1] == "pat" && segments[2] == "v1";
    }

    static bool IsLowerHex(string value, int length) => value.Length == length && value.All(character => character is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
}
