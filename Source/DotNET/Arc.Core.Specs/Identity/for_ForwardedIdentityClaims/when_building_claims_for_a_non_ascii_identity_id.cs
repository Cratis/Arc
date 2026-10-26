// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Identity.for_ForwardedIdentityClaims;

public class when_building_claims_for_a_non_ascii_identity_id : Specification
{
    List<Claim> _claims;

    void Because() => _claims = ForwardedIdentityClaims.From(new ClientPrincipal { UserId = "søren", UserDetails = "Søren" }, "UTF-8''s%C3%B8ren");

    [Fact] void should_decode_the_name_identifier() => _claims.Single(_ => _.Type == ClaimTypes.NameIdentifier).Value.ShouldEqual("søren");
    [Fact] void should_decode_the_subject() => _claims.Single(_ => _.Type == "sub").Value.ShouldEqual("søren");
}
