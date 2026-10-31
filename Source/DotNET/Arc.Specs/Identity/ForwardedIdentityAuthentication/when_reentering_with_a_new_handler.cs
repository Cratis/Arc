// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_reentering_with_a_new_handler : given.a_signed_handler
{
    AuthenticateResult _first;
    AuthenticateResult[] _reentries;
    AuthenticateResult _mutated;

    async Task Because()
    {
        _first = await Authenticate();
        ((ClaimsIdentity)_first.Principal.Identity).AddClaim(new Claim("injected", "yes"));
        _reentries = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Authenticate()));
        _context.Request.Headers[MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = "forged";
        _mutated = await Authenticate();
    }

    [Fact] void should_cache_beyond_the_handler_instance() => _reentries.All(result => result.Succeeded).ShouldBeTrue();
    [Fact] void should_return_independent_principals() => _reentries.Select(result => result.Principal).Distinct().Count().ShouldEqual(8);
    [Fact] void should_not_copy_caller_mutations() => _reentries.All(result => !result.Principal.HasClaim(claim => claim.Type == "injected")).ShouldBeTrue();
    [Fact] void should_invalidate_when_headers_change() => _mutated.Succeeded.ShouldBeFalse();
}
