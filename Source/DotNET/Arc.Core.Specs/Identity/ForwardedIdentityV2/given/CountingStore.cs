// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.ForwardedIdentityV2.given;

internal sealed class CountingStore : IForwardedIdentityNonceStore
{
    readonly IForwardedIdentityNonceStore _inner = new ProcessLocalForwardedIdentityNonceStore();
    internal int Calls { get; private set; }
    public Task<ForwardedIdentityNonceReservation> Reserve(string aud, string kid, string nonce, long timestamp, long now, CancellationToken cancellationToken)
    {
        Calls++;
        return _inner.Reserve(aud, kid, nonce, timestamp, now, cancellationToken);
    }
}
