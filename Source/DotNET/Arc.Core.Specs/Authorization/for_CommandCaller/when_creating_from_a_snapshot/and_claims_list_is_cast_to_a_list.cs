// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_CommandCaller.when_creating_from_a_snapshot;

public class and_claims_list_is_cast_to_a_list : Specification
{
    Exception _exception;

    void Because()
    {
        var caller = CommandCaller.From(AuthorizationPrincipalIdentity.Capture(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "represented")], "test"))));
        _exception = Catch.Exception(() => ((IList<CommandCallerClaim>)caller.Claims).Add(new CommandCallerClaim("sub", "injected", "issuer")));
    }

    [Fact] void should_refuse_mutation() => _exception.ShouldBeOfExactType<NotSupportedException>();
}
