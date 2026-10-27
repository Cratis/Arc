// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Cratis.Arc.Identity;

/// <summary>
/// Reads the configured on-behalf signing keys into RSA public key parameters, keyed by key identifier.
/// </summary>
/// <remarks>
/// The keys are read once per <see cref="OnBehalfOfOptions"/> instance and read again when the configured keys change.
/// A configuration that cannot be used - no issuer, audience or keys, a blank or duplicate key identifier, a key that is
/// not an RSA public key of at least 2048 bits, or a private key - makes every on-behalf forward fail.
/// </remarks>
internal sealed class OnBehalfOfSigningKeys
{
    /// <summary>
    /// The smallest RSA key size accepted.
    /// </summary>
    internal const int MinimumKeySize = 2048;

    static readonly ConditionalWeakTable<OnBehalfOfOptions, OnBehalfOfSigningKeys> _cache = new();

    readonly Dictionary<string, RSAParameters> _keys;

    OnBehalfOfSigningKeys(string fingerprint, Dictionary<string, RSAParameters> keys, string? problem)
    {
        Fingerprint = fingerprint;
        _keys = keys;
        Problem = problem;
    }

    /// <summary>
    /// Gets what makes the configuration unusable, or <see langword="null"/> when it is usable.
    /// </summary>
    internal string? Problem { get; }

    string Fingerprint { get; }

    /// <summary>
    /// Gets the signing keys for the given options.
    /// </summary>
    /// <param name="options">The <see cref="OnBehalfOfOptions"/>.</param>
    /// <returns>The <see cref="OnBehalfOfSigningKeys"/>.</returns>
    internal static OnBehalfOfSigningKeys For(OnBehalfOfOptions options)
    {
        var fingerprint = FingerprintOf(options);
        if (_cache.TryGetValue(options, out var cached) && cached.Fingerprint == fingerprint)
        {
            return cached;
        }

        var keys = Read(options, fingerprint);
        _cache.AddOrUpdate(options, keys);
        return keys;
    }

    /// <summary>
    /// Finds the public key for a key identifier.
    /// </summary>
    /// <param name="keyId">The key identifier.</param>
    /// <param name="key">The public key parameters, when found.</param>
    /// <returns>True when the key identifier is configured.</returns>
    internal bool TryGet(string keyId, out RSAParameters key) => _keys.TryGetValue(keyId, out key);

    static string FingerprintOf(OnBehalfOfOptions options) =>
        string.Join('\n', new[] { options.Issuer, options.Audience }.Concat((options.SigningKeys ?? []).Select(key => $"{key?.KeyId}\0{key?.PublicKeyPem}")));

    static OnBehalfOfSigningKeys Read(OnBehalfOfOptions options, string fingerprint)
    {
        var keys = new Dictionary<string, RSAParameters>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(options.Issuer) || string.IsNullOrWhiteSpace(options.Audience))
        {
            return new(fingerprint, keys, "an issuer and an audience are required");
        }

        if (options.SigningKeys is null || options.SigningKeys.Count == 0)
        {
            return new(fingerprint, keys, "at least one signing key is required");
        }

        foreach (var key in options.SigningKeys)
        {
            var problem = Read(key, out var parameters);
            if (problem is null && !keys.TryAdd(key.KeyId, parameters))
            {
                problem = $"the key identifier '{key.KeyId}' is configured more than once";
            }

            if (problem is not null)
            {
                return new(fingerprint, [], problem);
            }
        }

        return new(fingerprint, keys, null);
    }

    static string? Read(OnBehalfOfSigningKey? key, out RSAParameters parameters)
    {
        parameters = default;
        if (key is null || string.IsNullOrWhiteSpace(key.KeyId))
        {
            return "every signing key needs a key identifier";
        }

        if (string.IsNullOrWhiteSpace(key.PublicKeyPem))
        {
            return $"the signing key '{key.KeyId}' has no public key";
        }

        if (key.PublicKeyPem.Contains("PRIVATE KEY", StringComparison.OrdinalIgnoreCase))
        {
            return $"the signing key '{key.KeyId}' is a private key - configure the public key only";
        }

        using var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(key.PublicKeyPem);
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException)
        {
            return $"the signing key '{key.KeyId}' is not an RSA public key in PEM form";
        }

        if (rsa.KeySize < MinimumKeySize)
        {
            return $"the signing key '{key.KeyId}' is shorter than {MinimumKeySize} bits";
        }

        parameters = rsa.ExportParameters(includePrivateParameters: false);
        return null;
    }
}
