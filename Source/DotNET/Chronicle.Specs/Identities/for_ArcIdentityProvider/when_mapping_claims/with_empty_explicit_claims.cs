// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_mapping_claims;

public class with_empty_explicit_claims : Specification
{
    ChronicleIdentity _result;

    void Because() => _result = ArcIdentityProvider.FromPrincipal(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "subject"), new Claim("name", string.Empty), new Claim("preferred_username", string.Empty), new Claim(ClaimTypes.Name, "ignored")], "test")));

    [Fact] void should_not_replace_empty_claims_with_identity_name() => _result.ShouldEqual(new ChronicleIdentity("subject", string.Empty));
}
