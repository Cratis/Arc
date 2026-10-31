// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_ForwardedIdentityReplayCache;

public class when_reserving_a_future_dated_nonce : Specification
{
    bool _accepted;
    bool _full;
    bool _inclusive;
    bool _released;

    void Because()
    {
        var cache = new ForwardedIdentityReplayCache(1);
        var first = ForwardedIdentityEnvelope.Parse(ForwardedIdentityForSpecs.Envelope)! with { Timestamp = 1030 };
        var second = first with { Nonce = "different" };
        _accepted = cache.TryReserve("arc-test", first, 1000);
        _full = cache.TryReserve("arc-test", second, 1001);
        _inclusive = cache.TryReserve("arc-test", second, 1090);
        _released = cache.TryReserve("arc-test", second, 1091);
    }

    [Fact] void should_accept_the_first_reservation() => _accepted.ShouldBeTrue();
    [Fact] void should_fail_closed_at_capacity() => _full.ShouldBeFalse();
    [Fact] void should_retain_through_the_last_acceptable_second() => _inclusive.ShouldBeFalse();
    [Fact] void should_reclaim_only_expired_entries() => _released.ShouldBeTrue();
}
