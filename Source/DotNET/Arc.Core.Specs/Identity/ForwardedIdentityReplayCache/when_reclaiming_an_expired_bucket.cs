// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_ForwardedIdentityReplayCache;

public class when_reclaiming_an_expired_bucket : Specification
{
    bool _firstReused;
    bool _secondReused;
    bool _replay;

    void Because()
    {
        var cache = new ForwardedIdentityReplayCache(2);
        var first = ForwardedIdentityEnvelope.Parse(ForwardedIdentityForSpecs.Envelope) with { Timestamp = 1000 };
        var second = first with { Nonce = "second" };
        cache.TryReserve("arc-test", first, 1000);
        cache.TryReserve("arc-test", second, 1000);
        _firstReused = cache.TryReserve("arc-test", first with { Timestamp = 1061 }, 1061);
        _secondReused = cache.TryReserve("arc-test", second with { Timestamp = 1061 }, 1061);
        _replay = cache.TryReserve("arc-test", first with { Timestamp = 1061 }, 1061);
    }

    [Fact] void should_release_the_first_nonce() => _firstReused.ShouldBeTrue();
    [Fact] void should_release_every_nonce_in_the_expired_second() => _secondReused.ShouldBeTrue();
    [Fact] void should_reject_replay_after_reuse() => _replay.ShouldBeFalse();
}
