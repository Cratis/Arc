// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

namespace Cratis.Arc.Identity.for_ForwardedIdentityHttpListener;

public class when_enforcing_the_replay_window : given.a_listener
{
    AuthenticationResult _past;
    AuthenticationResult _future;
    AuthenticationResult _expired;
    AuthenticationResult _tooFarAhead;
    AuthenticationResult _replayed;

    async Task Because()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        _past = await Send(now - 30);
        _future = await Send(now + 15);
        _expired = await Send(now - 90);
        _tooFarAhead = await Send(now + 60);
        var headers = ForwardedIdentityForSpecs.Headers();
        headers[ForwardedIdentityForSpecs.SignatureHeader] = ForwardedIdentityForSpecs.Sign(headers);
        (await Authenticate(headers)).IsAuthenticated.ShouldBeTrue();
        _replayed = await Authenticate(headers);
    }

    [Fact] void should_accept_a_recent_timestamp_with_a_fresh_nonce() => _past.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_accept_future_timestamps_within_the_clock_allowance() => _future.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_reject_expired_envelopes() => _expired.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_reject_timestamps_beyond_the_future_allowance() => _tooFarAhead.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_reject_the_same_nonce_on_a_different_request() => _replayed.IsAuthenticated.ShouldBeFalse();

    Task<AuthenticationResult> Send(long timestamp)
    {
        var headers = ForwardedIdentityForSpecs.Headers();
        headers[ForwardedIdentityForSpecs.SignatureHeader] = ForwardedIdentityForSpecs.Sign(headers, timestamp: timestamp);
        return Authenticate(headers);
    }
}
