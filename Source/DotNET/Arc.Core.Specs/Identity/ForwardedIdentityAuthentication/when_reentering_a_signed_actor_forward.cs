// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_reentering_a_signed_actor_forward : given.a_signed_request
{
    DateTimeOffset _now;
    ForwardedIdentityAuthenticationResult _first;
    ForwardedIdentityAuthenticationResult _again;
    ForwardedIdentityAuthenticationResult _replayed;

    void Establish()
    {
        _now = DateTimeOffset.UtcNow;
        _clock.GetUtcNow().Returns(_now);
        ForwardedIdentityForSpecs.AddActor(_headers, _options, _now);
        Resign(_now.ToUnixTimeSeconds());
    }

    void Because()
    {
        _first = Authenticate();
        ((ClaimsIdentity)_first.Principal!.Identity!).Actor!.AddClaim(new Claim("injected", "yes"));
        _again = Authenticate();
        _context.Items.Returns(new Dictionary<object, object?>());
        Resign(_now.ToUnixTimeSeconds());
        _replayed = Authenticate();
    }

    [Fact] void should_attach_the_verified_actor() => ((ClaimsIdentity)_again.Principal!.Identity!).Actor!.FindFirst(ClaimTypes.NameIdentifier)!.Value.ShouldEqual("actor");
    [Fact] void should_keep_actor_roles_off_the_represented_identity() => _again.Principal!.IsInRole("administrator").ShouldBeFalse();
    [Fact] void should_clone_cached_actor_claims() => ((ClaimsIdentity)_again.Principal!.Identity!).Actor!.HasClaim(claim => claim.Type == "injected").ShouldBeFalse();
    [Fact] void should_not_repeat_the_inner_replay_check_on_reentry() => _again.Principal.ShouldNotBeNull();
    [Fact] void should_reject_replayed_inner_assertions_with_a_fresh_outer_nonce() => _replayed.Principal.ShouldBeNull();
}
