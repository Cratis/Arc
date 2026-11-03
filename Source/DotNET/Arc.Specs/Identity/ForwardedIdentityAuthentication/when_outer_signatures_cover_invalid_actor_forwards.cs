// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_outer_signatures_cover_invalid_actor_forwards : given.a_signed_handler
{
    readonly Dictionary<string, AuthenticateResult> _results = [];
    AuthenticateResult _recovered;

    async Task Because()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var scenario in ForwardedIdentityForSpecs.InvalidActorCases)
        {
            ResetRequest();
            _clock.GetUtcNow().Returns(now);
            ForwardedIdentityForSpecs.BreakActorForward(_headers, _options, scenario, now);
            _headers[ForwardedIdentityForSpecs.SignatureHeader] = ForwardedIdentityForSpecs.Sign(_headers, timestamp: now.ToUnixTimeSeconds());
            ApplyHeaders();
            _results[scenario] = await Authenticate();
            _results[scenario + " reentry"] = await Authenticate();
        }
        ResetRequest();
        _clock.GetUtcNow().Returns(now);
        ForwardedIdentityForSpecs.BreakActorForward(_headers, _options, "missing assertion", now);
        _headers[ForwardedIdentityForSpecs.SignatureHeader] = ForwardedIdentityForSpecs.Sign(_headers, timestamp: now.ToUnixTimeSeconds());
        ApplyHeaders();
        (await Authenticate()).Succeeded.ShouldBeFalse();
        ForwardedIdentityForSpecs.AddActor(_headers, _options, now);
        _headers[ForwardedIdentityForSpecs.SignatureHeader] = ForwardedIdentityForSpecs.Sign(_headers, timestamp: now.ToUnixTimeSeconds());
        ApplyHeaders();
        _recovered = await Authenticate();
    }

    [Fact] void should_reject_every_invalid_inner_protocol_even_with_a_valid_outer_mac() => _results.Where(pair => pair.Value.Succeeded).Select(pair => pair.Key).ShouldBeEmpty();
    [Fact] void should_never_cache_a_partial_outer_success() => _results.Values.All(result => result.Principal is null).ShouldBeTrue();
    [Fact] void should_allow_a_corrected_forward_with_fresh_credentials_on_the_same_request() => _recovered.Succeeded.ShouldBeTrue();
}
