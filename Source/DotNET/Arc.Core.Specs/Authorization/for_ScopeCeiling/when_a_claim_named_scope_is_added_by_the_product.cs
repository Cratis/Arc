// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization.given;

namespace Cratis.Arc.Authorization.for_ScopeCeiling;

public class when_a_claim_named_scope_is_added_by_the_product : Specification
{
    bool _result;

    void Because()
    {
        var principal = verified_callers.PrincipalFor(verified_callers.Person("person"));
        principal.AddIdentity(new ClaimsIdentity([new Claim("scope", "a:read")], "product"));
        ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim("scope", "a:read"));
        _result = ScopeCeiling.Allows(principal, ["a:read"]);
    }

    [Fact] void should_still_refuse_because_the_ceiling_reads_the_caller_never_claims() => _result.ShouldBeFalse();
}
