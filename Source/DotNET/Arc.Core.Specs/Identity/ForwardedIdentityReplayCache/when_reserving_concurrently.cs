// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_ForwardedIdentityReplayCache;

public class when_reserving_concurrently : Specification
{
    bool[] _results;

    async Task Because()
    {
        var cache = new ForwardedIdentityReplayCache();
        var envelope = ForwardedIdentityEnvelope.Parse(ForwardedIdentityForSpecs.Envelope)!;
        _results = await Task.WhenAll(Enumerable.Range(0, 64).Select(_ => Task.Run(() => cache.TryReserve("arc-test", envelope, 1700000000))));
    }

    [Fact] void should_allow_exactly_one_request() => _results.Count(result => result).ShouldEqual(1);
}
