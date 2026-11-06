// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>Verifies the complete v2 contract before reserving a nonce or returning facts.</summary>
/// <param name="nonces">The explicitly selected replay store.</param>
/// <param name="bearers">The independent bearer verifier.</param>
/// <param name="time">The trusted clock.</param>
internal sealed class ForwardedIdentityV2Verifier(IForwardedIdentityNonceStore nonces, IForwardedBearerVerifier bearers, TimeProvider time)
{
    /// <summary>Verifies an immutable request against explicit installation configuration.</summary>
    /// <param name="settings">The explicit verification settings.</param>
    /// <param name="request">The immutable raw snapshot.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A complete result or fixed refusal reason.</returns>
    internal async Task<ForwardedIdentityV2Verification> Verify(ForwardedIdentityV2Settings settings, ForwardedIdentityV2Request request, CancellationToken cancellationToken)
    {
        if (!request.AuthenticatedHop) return ForwardedIdentityV2Verification.Rejected(ForwardedIdentityV2Rejection.Hop);
        try
        {
            var envelope = ReadEnvelope(request);
            var now = time.GetUtcNow().ToUnixTimeSeconds();
            if (envelope.Timestamp < (decimal)now - 60 || envelope.Timestamp > (decimal)now + 30) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Freshness);
            if (!settings.Keys.TryGetValue(envelope.KeyId, out var key)) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Key);
            var (bytes, headers) = ForwardedIdentityV2Canonicalization.Create(settings, request, envelope.Parts);
            CheckSignature(key, bytes, envelope.Signature);
            ForwardedIdentityV2Principal.Validate(headers);
            var context = ForwardedIdentityV2Context.Create(headers, settings.RealmIssuer, settings.TenantHeader);
            var bearer = await ForwardedBearerBinding.Bind(request, context, bearers, cancellationToken);
            if (request.ReplaySensitive) await Reserve(settings.Audience, envelope, now, cancellationToken);
            return ForwardedIdentityV2Verification.Accepted(context, bearer);
        }
        catch (Exception error) when (ForwardedIdentityV2Rejected.Is(error, out var reason))
        {
            return ForwardedIdentityV2Verification.Rejected(reason);
        }
    }

    static ForwardedIdentityV2Envelope ReadEnvelope(ForwardedIdentityV2Request request)
    {
        var signatures = request.Headers.Where(header => header.Name.Equals(ForwardedIdentityV2Envelope.Header, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (signatures.Length != 1) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Format);
        var parsed = ForwardedIdentityV2Envelope.Parse(signatures[0].Value);
        if (parsed.Rejection is { } reason) throw ForwardedIdentityV2Rejected.For(reason);
        return parsed.Envelope!;
    }

    static void CheckSignature(ForwardedIdentityV2PublicKey key, byte[] bytes, byte[] signature)
    {
        var verified = false;
        try
        {
            verified = key.Verify(bytes, signature);
        }
        catch (CryptographicException)
        {
            // Invalid scalars are a uniform signature refusal.
        }
        if (!verified) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Signature);
    }

    async Task Reserve(string audience, ForwardedIdentityV2Envelope envelope, long now, CancellationToken cancellationToken)
    {
        ForwardedIdentityNonceReservation reservation;
        try
        {
            reservation = await nonces.Reserve(audience, envelope.KeyId, envelope.Parts[3], envelope.Timestamp, now, cancellationToken);
        }
        catch (Exception)
        {
            throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Store);
        }
        if (reservation == ForwardedIdentityNonceReservation.Replayed) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Replay);
        if (reservation != ForwardedIdentityNonceReservation.Reserved) throw ForwardedIdentityV2Rejected.For(ForwardedIdentityV2Rejection.Store);
    }
}
