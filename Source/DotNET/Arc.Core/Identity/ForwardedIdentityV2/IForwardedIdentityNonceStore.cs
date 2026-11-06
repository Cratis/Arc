// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>Atomically reserves nonces until the inclusive timestamp-plus-60 boundary.</summary>
public interface IForwardedIdentityNonceStore
{
    /// <summary>Reserves a nonce without evicting live reservations.</summary>
    /// <param name="aud">The configured audience.</param>
    /// <param name="kid">The signing key identifier.</param>
    /// <param name="nonce">The canonical nonce text.</param>
    /// <param name="timestamp">The verified envelope timestamp.</param>
    /// <param name="now">The current Unix time.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The reservation outcome.</returns>
    Task<ForwardedIdentityNonceReservation> Reserve(string aud, string kid, string nonce, long timestamp, long now, CancellationToken cancellationToken);
}
