// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cratis.Arc.Identity;

/// <summary>
/// Verifies the signature, issuer, audience, lifetime and purpose of an on-behalf assertion.
/// </summary>
/// <remarks>
/// The assertion is a compact RS256 JWS. Only RS256 is accepted, the key is selected by <c>kid</c> from the pinned
/// <see cref="OnBehalfOfOptions.SigningKeys"/>, and a token whose lifetime (<c>exp</c> minus <c>nbf</c>, or minus
/// <c>iat</c> when present) exceeds <see cref="MaximumLifetime"/> is refused.
/// </remarks>
internal static class OnBehalfOfAssertionVerifier
{
    /// <summary>
    /// The purpose an assertion for a forwarded request carries.
    /// </summary>
    internal const string ForwardPurpose = "on-behalf-forward";

    /// <summary>
    /// The longest lifetime an assertion may have.
    /// </summary>
    internal static readonly TimeSpan MaximumLifetime = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Verifies an assertion.
    /// </summary>
    /// <param name="token">The compact JWS.</param>
    /// <param name="keys">The <see cref="OnBehalfOfSigningKeys"/> to verify with.</param>
    /// <param name="options">The <see cref="OnBehalfOfOptions"/>.</param>
    /// <param name="now">The current time.</param>
    /// <param name="assertion">The verified assertion, when verification succeeds.</param>
    /// <returns>The failure reason, or <see langword="null"/> when the assertion verified.</returns>
    internal static string? Verify(string token, OnBehalfOfSigningKeys keys, OnBehalfOfOptions options, DateTimeOffset now, out OnBehalfOfAssertion? assertion)
    {
        assertion = null;
        var parts = token.Split('.');
        if (parts.Length != 3 ||
            !OnBehalfOfBase64Url.TryDecode(parts[0], out var headerBytes) ||
            !OnBehalfOfBase64Url.TryDecode(parts[1], out var payloadBytes) ||
            !OnBehalfOfBase64Url.TryDecode(parts[2], out var signature))
        {
            return OnBehalfOfFailures.MalformedAssertion;
        }

        try
        {
            using var header = JsonDocument.Parse(headerBytes);
            using var payload = JsonDocument.Parse(payloadBytes);
            if (header.RootElement.ValueKind != JsonValueKind.Object || payload.RootElement.ValueKind != JsonValueKind.Object)
            {
                return OnBehalfOfFailures.MalformedAssertion;
            }

            var failure = VerifySignature(header.RootElement, parts, signature, keys);
            return failure ?? VerifyClaims(payload.RootElement, options, now, out assertion);
        }
        catch (JsonException)
        {
            return OnBehalfOfFailures.MalformedAssertion;
        }
    }

    static string? VerifySignature(JsonElement header, string[] parts, byte[] signature, OnBehalfOfSigningKeys keys)
    {
        if (header.TryGetProperty("crit", out _))
        {
            return OnBehalfOfFailures.MalformedAssertion;
        }

        if (OnBehalfOfClaims.String(header, "alg") != "RS256")
        {
            return OnBehalfOfFailures.UnsupportedAlgorithm;
        }

        var keyId = OnBehalfOfClaims.String(header, "kid");
        if (string.IsNullOrEmpty(keyId) || !keys.TryGet(keyId, out var parameters))
        {
            return OnBehalfOfFailures.UnknownSigningKey;
        }

        using var rsa = RSA.Create(parameters);
        var signed = Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}");
        return rsa.VerifyData(signed, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            ? null
            : OnBehalfOfFailures.InvalidSignature;
    }

    static string? VerifyClaims(JsonElement payload, OnBehalfOfOptions options, DateTimeOffset now, out OnBehalfOfAssertion? assertion)
    {
        assertion = null;
        if (OnBehalfOfClaims.String(payload, "iss") != options.Issuer)
        {
            return OnBehalfOfFailures.InvalidIssuer;
        }

        if (!OnBehalfOfClaims.HasAudience(payload, options.Audience))
        {
            return OnBehalfOfFailures.InvalidAudience;
        }

        var lifetimeFailure = VerifyLifetime(payload, options.ClockSkew, now, out var expires);
        if (lifetimeFailure is not null)
        {
            return lifetimeFailure;
        }

        if (OnBehalfOfClaims.String(payload, "purpose") != ForwardPurpose)
        {
            return OnBehalfOfFailures.WrongPurpose;
        }

        var tokenId = OnBehalfOfClaims.String(payload, "jti");
        if (string.IsNullOrEmpty(tokenId))
        {
            return OnBehalfOfFailures.MissingTokenId;
        }

        var subject = OnBehalfOfClaims.String(payload, "sub");
        var principalHash = OnBehalfOfClaims.String(payload, "principal_hash");
        if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(principalHash))
        {
            return OnBehalfOfFailures.PrincipalMismatch;
        }

        var actorSubject = payload.TryGetProperty("act", out var act) && act.ValueKind == JsonValueKind.Object ? OnBehalfOfClaims.String(act, "sub") : null;
        var actorHash = OnBehalfOfClaims.String(payload, "actor_hash");
        if (string.IsNullOrEmpty(actorSubject) || string.IsNullOrEmpty(actorHash))
        {
            return OnBehalfOfFailures.ActorMismatch;
        }

        assertion = new(
            subject,
            actorSubject,
            OnBehalfOfClaims.String(payload, "tenant"),
            principalHash,
            actorHash,
            OnBehalfOfClaims.String(payload, "htm"),
            OnBehalfOfClaims.String(payload, "htu"),
            tokenId,
            expires);
        return null;
    }

    static string? VerifyLifetime(JsonElement payload, TimeSpan clockSkew, DateTimeOffset now, out DateTimeOffset expires)
    {
        expires = default;
        var notBefore = OnBehalfOfClaims.Time(payload, "nbf");
        var expiresAt = OnBehalfOfClaims.Time(payload, "exp");
        if (notBefore is null || expiresAt is null)
        {
            return OnBehalfOfFailures.MissingLifetime;
        }

        expires = expiresAt.Value;
        var issuedAt = OnBehalfOfClaims.Time(payload, "iat") ?? notBefore.Value;
        var start = issuedAt < notBefore.Value ? issuedAt : notBefore.Value;
        if (expires - start > MaximumLifetime)
        {
            return OnBehalfOfFailures.LifetimeTooLong;
        }

        if (now + clockSkew < notBefore.Value)
        {
            return OnBehalfOfFailures.NotYetValid;
        }

        return now - clockSkew >= expires ? OnBehalfOfFailures.Expired : null;
    }
}
