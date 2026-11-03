// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_ForwardedIdentityReplayCache;

public class when_expirations_arrive_out_of_order : Specification
{
    bool _first;
    bool _second;
    bool _replay;
    bool _atExpiry;
    bool _reused;
    bool _liveReplay;
    bool _full;

    void Because()
    {
        var cache = new ForwardedIdentityReplayCache(2);
        var later = ForwardedIdentityEnvelope.Parse(ForwardedIdentityForSpecs.Envelope) with { Timestamp = 1030 };
        var earlier = later with { Timestamp = 1000, Nonce = "earlier" };
        _first = cache.TryReserve("arc-test", later, 1000);
        _second = cache.TryReserve("arc-test", earlier, 1000);
        _replay = cache.TryReserve("arc-test", earlier, 1001);
        _atExpiry = cache.TryReserve("arc-test", earlier, 1060);
        _reused = cache.TryReserve("arc-test", earlier with { Timestamp = 1061 }, 1061);
        _liveReplay = cache.TryReserve("arc-test", later, 1061);
        _full = cache.TryReserve("arc-test", later with { Nonce = "third" }, 1061);
    }

    [Fact] void should_reserve_the_later_expiration() => _first.ShouldBeTrue();
    [Fact] void should_reserve_the_earlier_expiration() => _second.ShouldBeTrue();
    [Fact] void should_reject_a_reserved_nonce() => _replay.ShouldBeFalse();
    [Fact] void should_reject_reuse_at_the_inclusive_expiry() => _atExpiry.ShouldBeFalse();
    [Fact] void should_release_the_earlier_nonce_after_expiry() => _reused.ShouldBeTrue();
    [Fact] void should_preserve_the_unexpired_reservation() => _liveReplay.ShouldBeFalse();
    [Fact] void should_fail_closed_after_reclaiming_only_expired_entries() => _full.ShouldBeFalse();
}
