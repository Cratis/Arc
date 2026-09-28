// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_AuthorizationPrincipalIdentity;

public class when_standard_claim_content_changes : Specification
{
    bool _same;

    void Because()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("permission", "reader")], "test"));
        var snapshot = AuthorizationPrincipalIdentity.Capture(principal);
        principal.Identities.Single().AddClaim(new Claim("permission", "writer"));
        _same = AuthorizationPrincipalIdentity.Same(snapshot, principal);
    }

    [Fact] void should_detect_the_change() => _same.ShouldBeFalse();
}
