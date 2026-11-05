// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ProcessLocalForwardedIdentityNonceStore;

public class when_reserving : Specification
{
    ForwardedIdentityNonceReservation[] _concurrent;
    readonly List<ForwardedIdentityNonceReservation> _outcomes = [];

    async Task Because()
    {
        IForwardedIdentityNonceStore store = new ProcessLocalForwardedIdentityNonceStore(1);
        _concurrent = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() => store.Reserve("aud", "kid", "nonce", 100, 100, CancellationToken.None))));
        _outcomes.Add(await store.Reserve("aud", "kid", "other", 100, 100, CancellationToken.None));
        _outcomes.Add(await store.Reserve("aud", "kid", "nonce", 100, 160, CancellationToken.None));
        _outcomes.Add(await store.Reserve("aud", "kid", "next", 161, 161, CancellationToken.None));
        _outcomes.Add(await store.Reserve("aud", "kid", "next", 161, 161, CancellationToken.None));
        IForwardedIdentityNonceStore isolated = new ProcessLocalForwardedIdentityNonceStore();
        _outcomes.Add(await isolated.Reserve("a", "k", "n", 0, 0, CancellationToken.None));
        _outcomes.Add(await isolated.Reserve("b", "k", "n", 0, 0, CancellationToken.None));
        _outcomes.Add(await isolated.Reserve("a", "other-k", "n", 0, 0, CancellationToken.None));
    }

    [Fact] void should_reserve_exactly_once_under_contention() => _concurrent.Count(result => result == ForwardedIdentityNonceReservation.Reserved).ShouldEqual(1);
    [Fact] void should_refuse_all_concurrent_replays() => _concurrent.Count(result => result == ForwardedIdentityNonceReservation.Replayed).ShouldEqual(31);
    [Fact] void should_fail_full_without_eviction_and_keep_the_inclusive_tail() => _outcomes.ShouldEqual(new[] { ForwardedIdentityNonceReservation.Unavailable, ForwardedIdentityNonceReservation.Replayed, ForwardedIdentityNonceReservation.Reserved, ForwardedIdentityNonceReservation.Replayed, ForwardedIdentityNonceReservation.Reserved, ForwardedIdentityNonceReservation.Reserved, ForwardedIdentityNonceReservation.Reserved });
}
