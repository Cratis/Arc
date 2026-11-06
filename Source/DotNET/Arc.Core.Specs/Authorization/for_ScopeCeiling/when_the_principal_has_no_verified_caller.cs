// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_ScopeCeiling;

public class when_the_principal_has_no_verified_caller : Specification
{
    bool _result;

    void Because() => _result = ScopeCeiling.Allows(new ClaimsPrincipal(new ClaimsIdentity([new Claim("scope", "content:write")], "other")), ["content:write"]);

    [Fact] void should_allow_because_the_ceiling_only_narrows_delegated_callers() => _result.ShouldBeTrue();
}
