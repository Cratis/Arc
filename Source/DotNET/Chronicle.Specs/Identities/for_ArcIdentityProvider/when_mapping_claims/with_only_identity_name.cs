// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_mapping_claims;

public class with_only_identity_name : Specification
{
    ChronicleIdentity _result;

    void Because() => _result = ArcIdentityProvider.FromPrincipal(new ClaimsPrincipal(new ClaimsIdentity([new Claim("custom-name", "identity-name"), new Claim(ClaimTypes.NameIdentifier, "not-sub")], "test", "custom-name", ClaimTypes.Role)));

    [Fact] void should_fall_back_to_identity_name_for_both_names() => _result.ShouldEqual(new ChronicleIdentity(string.Empty, "identity-name", "identity-name"));
}
