// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_enforcing_the_replay_window : given.a_signed_handler
{
    readonly Dictionary<long, AuthenticateResult> _results = [];
    AuthenticateResult _replayed;

    async Task Because()
    {
        foreach (var offset in new long[] { -60, 30, -61, 31 })
        {
            ResetRequest();
            _headers[ForwardedIdentityForSpecs.SignatureHeader] = ForwardedIdentityForSpecs.Sign(_headers, timestamp: 1700000000 + offset);
            ApplyHeaders();
            _results[offset] = await Authenticate();
        }
        ResetRequest();
        (await Authenticate()).Succeeded.ShouldBeTrue();
        _context.Items = new Dictionary<object, object?>();
        _replayed = await Authenticate();
    }

    [Fact] void should_accept_the_oldest_timestamp_inclusively() => _results[-60].Succeeded.ShouldBeTrue();
    [Fact] void should_accept_the_future_allowance_inclusively() => _results[30].Succeeded.ShouldBeTrue();
    [Fact] void should_reject_expired_envelopes() => _results[-61].Succeeded.ShouldBeFalse();
    [Fact] void should_reject_timestamps_beyond_the_future_allowance() => _results[31].Succeeded.ShouldBeFalse();
    [Fact] void should_reject_the_same_nonce_on_a_distinct_request() => _replayed.Succeeded.ShouldBeFalse();
}
