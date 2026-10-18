// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_mapping_claims;

public class with_preferred_username_claims : Specification
{
    ChronicleIdentity _result;

    void Because() => _result = ArcIdentityProvider.FromPrincipal(new ClaimsPrincipal(new ClaimsIdentity([new Claim("preferred_username", "first"), new Claim("preferred_username", "second"), new Claim(ClaimTypes.Name, "identity-name")], "test")));

    [Fact] void should_prefer_the_first_username_claim_over_identity_name() => _result.ShouldEqual(new ChronicleIdentity(string.Empty, "identity-name", "first"));
}
