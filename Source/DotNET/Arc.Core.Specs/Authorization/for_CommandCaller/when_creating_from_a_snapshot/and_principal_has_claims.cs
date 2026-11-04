// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_CommandCaller.when_creating_from_a_snapshot;

public class and_principal_has_claims : Specification
{
    CommandCaller _caller;

    void Because()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "represented", ClaimValueTypes.String, "issuer")], "test"));
        var snapshot = AuthorizationPrincipalIdentity.Capture(principal);
        ((ClaimsIdentity)principal.Identity).AddClaim(new Claim("sub", "later"));
        _caller = CommandCaller.From(snapshot);
    }

    [Fact] void should_preserve_captured_claims() => _caller.Claims.ShouldContainOnly(new CommandCallerClaim("sub", "represented", "issuer"));
    [Fact] void should_be_authenticated() => _caller.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_preserve_authentication_type() => _caller.AuthenticationType.ShouldEqual("test");
}
