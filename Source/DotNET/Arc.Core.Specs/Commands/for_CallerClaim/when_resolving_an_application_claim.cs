// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Commands.for_CallerClaim;

public class when_resolving_an_application_claim : Specification
{
    ReadModelKeyLookup _result;

    void Because()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("login", "member")], "test"));
        var caller = CommandCaller.From(AuthorizationPrincipalIdentity.Capture(principal));
        ILookUpByKeyProvider attribute = new LookUpByAttribute<LoginClaim>();
        _result = attribute.Provider.Resolve(new ReadModelKeyContext(caller, typeof(object)));
    }

    [Fact] void should_find_the_login() => _result.Key.ShouldEqual("member");

    public sealed class LoginClaim() : CallerClaim("login");
}
