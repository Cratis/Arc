// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.ForwardedIdentityV2.given;

internal sealed class VectorNonceStore(IForwardedIdentityNonceStore inner, bool available, bool full) : IForwardedIdentityNonceStore
{
    public Task<ForwardedIdentityNonceReservation> Reserve(string aud, string kid, string nonce, long timestamp, long now, CancellationToken cancellationToken) => available && !full ? inner.Reserve(aud, kid, nonce, timestamp, now, cancellationToken) : Task.FromResult(ForwardedIdentityNonceReservation.Unavailable);
}
