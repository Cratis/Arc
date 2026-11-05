// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.ForwardedIdentityV2.given;

internal sealed class ThrowingStore : IForwardedIdentityNonceStore
{
    public Task<ForwardedIdentityNonceReservation> Reserve(string aud, string kid, string nonce, long timestamp, long now, CancellationToken cancellationToken) => throw new InvalidOperationException("Synthetic store failure.");
}
