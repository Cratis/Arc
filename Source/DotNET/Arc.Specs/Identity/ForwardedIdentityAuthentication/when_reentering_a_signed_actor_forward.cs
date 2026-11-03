// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_reentering_a_signed_actor_forward : given.a_signed_handler
{
    DateTimeOffset _now;
    AuthenticateResult _again;
    AuthenticateResult _replayed;

    void Establish()
    {
        _now = DateTimeOffset.UtcNow;
        _clock.GetUtcNow().Returns(_now);
        ForwardedIdentityForSpecs.AddActor(_headers, _options, _now);
        _headers[ForwardedIdentityForSpecs.SignatureHeader] = ForwardedIdentityForSpecs.Sign(_headers, timestamp: _now.ToUnixTimeSeconds());
        ApplyHeaders();
    }

    async Task Because()
    {
        var first = await Authenticate();
        ((ClaimsIdentity)first.Principal.Identity).Actor.AddClaim(new Claim("injected", "yes"));
        _again = await Authenticate();
        _context.Items = new Dictionary<object, object?>();
        _headers[ForwardedIdentityForSpecs.SignatureHeader] = ForwardedIdentityForSpecs.Sign(_headers, timestamp: _now.ToUnixTimeSeconds());
        ApplyHeaders();
        _replayed = await Authenticate();
    }

    [Fact] void should_cache_the_complete_actor_authentication() => _again.Succeeded.ShouldBeTrue();
    [Fact] void should_attach_the_verified_actor() => ((ClaimsIdentity)_again.Principal.Identity).Actor.FindFirst(ClaimTypes.NameIdentifier).Value.ShouldEqual("actor");
    [Fact] void should_not_augment_the_represented_roles() => _again.Principal.IsInRole("administrator").ShouldBeFalse();
    [Fact] void should_return_independent_actor_claims() => ((ClaimsIdentity)_again.Principal.Identity).Actor.HasClaim(claim => claim.Type == "injected").ShouldBeFalse();
    [Fact] void should_not_cache_a_success_on_another_request() => _replayed.Succeeded.ShouldBeFalse();
}
