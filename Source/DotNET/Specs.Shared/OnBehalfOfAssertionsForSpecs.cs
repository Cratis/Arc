// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cratis.Arc.Identity;

/// <summary>
/// Issues on-behalf assertions the way Cratis AuthProxy does, for specs that exercise on-behalf forwards.
/// </summary>
/// <remarks>
/// Linked into every spec project that needs a signed forward, so they all pin the same wire contract.
/// </remarks>
internal static class OnBehalfOfAssertionsForSpecs
{
    /// <summary>
    /// The issuer the specs configure and sign with.
    /// </summary>
    internal const string Issuer = "https://authproxy.example.com";

    /// <summary>
    /// The audience the specs configure and sign for.
    /// </summary>
    internal const string Audience = "https://app.example.com";

    /// <summary>
    /// The identifier of <see cref="SigningKey"/>.
    /// </summary>
    internal const string KeyId = "authproxy-2026";

    static readonly object _keyLock = new();

    /// <summary>
    /// Gets the key AuthProxy signs with in the specs.
    /// </summary>
    internal static RSA SigningKey { get; } = RSA.Create(2048);

    /// <summary>
    /// Gets the public key of <see cref="SigningKey"/> in PEM form.
    /// </summary>
    internal static string PublicKeyPem { get; } = SigningKey.ExportSubjectPublicKeyInfoPem();

    /// <summary>
    /// Gets a key that is not configured on the host.
    /// </summary>
    internal static RSA UnknownKey { get; } = RSA.Create(2048);

    /// <summary>
    /// Creates options that accept assertions signed by <see cref="SigningKey"/>.
    /// </summary>
    /// <returns>The <see cref="OnBehalfOfOptions"/>.</returns>
    internal static OnBehalfOfOptions Options() => new()
    {
        Enabled = true,
        Issuer = Issuer,
        Audience = Audience,
        SigningKeys = [new() { KeyId = KeyId, PublicKeyPem = PublicKeyPem }]
    };

    /// <summary>
    /// Creates the claims AuthProxy puts in a forward assertion.
    /// </summary>
    /// <param name="principalHeader">The exact principal header value.</param>
    /// <param name="actorHeader">The exact actor header value.</param>
    /// <param name="subject">The represented user's subject.</param>
    /// <param name="actorSubject">The actor's subject.</param>
    /// <param name="now">The issue time; defaults to now.</param>
    /// <returns>The claims, which a spec may change before signing. A claim set to null is left out.</returns>
    internal static Dictionary<string, object?> ClaimsFor(string principalHeader, string actorHeader, string subject, string actorSubject, DateTimeOffset? now = null)
    {
        var issuedAt = (now ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();
        return new()
        {
            ["iss"] = Issuer,
            ["aud"] = Audience,
            ["iat"] = issuedAt,
            ["nbf"] = issuedAt,
            ["exp"] = issuedAt + 60,
            ["jti"] = Guid.NewGuid().ToString("N"),
            ["purpose"] = "on-behalf-forward",
            ["sub"] = subject,
            ["act"] = new Dictionary<string, object> { ["sub"] = actorSubject, ["idp"] = "aad" },
            ["issue"] = "issue-42",
            ["sid"] = "session-42",
            ["principal_hash"] = Sha256(principalHeader),
            ["actor_hash"] = Sha256(actorHeader)
        };
    }

    /// <summary>
    /// Signs claims as a compact JWS.
    /// </summary>
    /// <param name="claims">The claims.</param>
    /// <param name="keyId">The <c>kid</c> header.</param>
    /// <param name="key">The key to sign with; defaults to <see cref="SigningKey"/>.</param>
    /// <param name="algorithm">The <c>alg</c> header.</param>
    /// <returns>The compact JWS.</returns>
    internal static string Sign(IDictionary<string, object?> claims, string keyId = KeyId, RSA? key = null, string algorithm = "RS256")
    {
        var header = Encode(JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, string> { ["alg"] = algorithm, ["kid"] = keyId, ["typ"] = "JWT" }));
        var payload = Encode(JsonSerializer.SerializeToUtf8Bytes(claims.Where(_ => _.Value is not null).ToDictionary(_ => _.Key, _ => _.Value)));

        // An RSA instance is not safe to use from parallel specs, so each signature uses its own copy of the key.
        RSAParameters parameters;
        lock (_keyLock)
        {
            parameters = (key ?? SigningKey).ExportParameters(includePrivateParameters: true);
        }

        using var signer = RSA.Create(parameters);
        var signature = signer.SignData(Encoding.ASCII.GetBytes($"{header}.{payload}"), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return $"{header}.{payload}.{Encode(signature)}";
    }

    /// <summary>
    /// Computes the unpadded base64url SHA-256 of a header value, as AuthProxy does.
    /// </summary>
    /// <param name="headerValue">The header value.</param>
    /// <returns>The digest.</returns>
    internal static string Sha256(string headerValue) => Encode(SHA256.HashData(Encoding.ASCII.GetBytes(headerValue)));

    static string Encode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
