// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_outer_signatures_cover_invalid_actor_forwards : given.a_signed_request
{
    readonly Dictionary<string, ForwardedIdentityAuthenticationResult> _results = [];
    ForwardedIdentityAuthenticationResult _recovered;

    void Because()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var scenario in ForwardedIdentityForSpecs.InvalidActorCases)
        {
            ResetRequest();
            _clock.GetUtcNow().Returns(now);
            ForwardedIdentityForSpecs.BreakActorForward(_headers, _options, scenario, now);
            Resign(now.ToUnixTimeSeconds());
            _results[scenario] = Authenticate();
            _results[scenario + " reentry"] = Authenticate();
        }
        ResetRequest();
        _clock.GetUtcNow().Returns(now);
        ForwardedIdentityForSpecs.BreakActorForward(_headers, _options, "missing assertion", now);
        Resign(now.ToUnixTimeSeconds());
        Authenticate().Principal.ShouldBeNull();
        ForwardedIdentityForSpecs.AddActor(_headers, _options, now);
        Resign(now.ToUnixTimeSeconds());
        _recovered = Authenticate();
    }

    [Fact] void should_not_authenticate_any_invalid_combined_forward() => _results.Where(pair => pair.Value.Principal is not null).Select(pair => pair.Key).ShouldBeEmpty();
    [Fact] void should_never_cache_a_partial_outer_success() => _results.Values.All(result => result.Failure is not null).ShouldBeTrue();
    [Fact] void should_allow_a_corrected_forward_with_fresh_credentials_on_the_same_request() => _recovered.Principal.ShouldNotBeNull();
}
