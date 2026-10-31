// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Arc.Identity;

/// <summary>
/// Holds validated integration keys and an exact private authentication-options snapshot.
/// </summary>
/// <param name="Keys">The decoded keys indexed by case-sensitive ID.</param>
/// <param name="Snapshot">The private framed configuration snapshot.</param>
internal sealed record ForwardedIdentityConfiguration(IReadOnlyDictionary<string, byte[]> Keys, string Snapshot)
{
    /// <summary>
    /// Validates opt-in configuration, failing closed on any malformed key entry.
    /// </summary>
    /// <param name="options">The host configuration.</param>
    /// <returns>The validated configuration, or null if invalid.</returns>
    internal static ForwardedIdentityConfiguration? Read(ArcOptions options)
    {
        var forwarded = options.ForwardedIdentity;
        if (!options.TrustForwardedIdentityHeaders || forwarded is not { Enabled: true } ||
            string.IsNullOrWhiteSpace(forwarded.Audience) || forwarded.Audience.Length > 128 ||
            forwarded.Audience.Any(character => character is < ' ' or >= (char)127) ||
            forwarded.Keys is not { Count: >= 1 and <= 8 } || options.OnBehalfOf?.SigningKeys is null || options.Tenancy is null)
        {
            return null;
        }
        var keys = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var snapshot = new StringBuilder();
        foreach (var value in new[]
        {
            options.TrustForwardedIdentityHeaders.ToString(), forwarded.Enabled.ToString(), forwarded.Audience, options.Tenancy.HttpHeader,
            options.OnBehalfOf.Enabled.ToString(), options.OnBehalfOf.Issuer, options.OnBehalfOf.Audience, options.OnBehalfOf.Service,
            options.OnBehalfOf.ClockSkew.Ticks.ToString(CultureInfo.InvariantCulture)
        })
        {
            snapshot.Append(ForwardedIdentityCanonicalization.Frame(value ?? string.Empty));
        }
        snapshot.Append(ForwardedIdentityCanonicalization.Frame(forwarded.Keys.Count.ToString(CultureInfo.InvariantCulture)));
        foreach (var key in forwarded.Keys)
        {
            if (key is null || !ForwardedIdentityEnvelope.IsKeyId(key.KeyId) || key.SecretBase64 is not { Length: 44 })
            {
                return null;
            }
            var bytes = new byte[32];
            if (!Convert.TryFromBase64String(key.SecretBase64, bytes, out var count) || count != 32 ||
                Convert.ToBase64String(bytes) != key.SecretBase64 || !keys.TryAdd(key.KeyId, bytes))
            {
                return null;
            }
            snapshot.Append(ForwardedIdentityCanonicalization.Frame(key.KeyId)).Append(ForwardedIdentityCanonicalization.Frame(Convert.ToHexString(SHA256.HashData(bytes))));
        }
        snapshot.Append(ForwardedIdentityCanonicalization.Frame(options.OnBehalfOf.SigningKeys.Count.ToString(CultureInfo.InvariantCulture)));
        foreach (var key in options.OnBehalfOf.SigningKeys)
        {
            if (key is null)
            {
                return null;
            }
            snapshot.Append(ForwardedIdentityCanonicalization.Frame(key.KeyId ?? string.Empty)).Append(ForwardedIdentityCanonicalization.Frame(key.PublicKeyPem ?? string.Empty));
        }
        return new(keys, snapshot.ToString());
    }
}
