// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_mapping_claims;

public class with_subject_claims : Specification
{
    ChronicleIdentity _result;

    void Because() => _result = ArcIdentityProvider.FromPrincipal(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "first"), new Claim("sub", "second"), new Claim(ClaimTypes.NameIdentifier, "identifier")], "test")));

    [Fact] void should_use_the_first_sub_claim() => _result.ShouldEqual(new ChronicleIdentity("first", string.Empty));
}
