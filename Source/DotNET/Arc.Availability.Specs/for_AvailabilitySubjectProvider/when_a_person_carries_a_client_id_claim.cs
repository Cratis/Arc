// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Tenancy;
using Cratis.Choreography.Availability.Audiences;

namespace Cratis.Arc.Availability.for_AvailabilitySubjectProvider;

public class when_a_person_carries_a_client_id_claim : Specification
{
    AudienceSubject _subject;

    void Because()
    {
        var tenant = Substitute.For<ITenantIdAccessor>();
        tenant.Current.Returns(TenantId.NotSet);
        var principal = Substitute.For<ICurrentPrincipalAccessor>();
        principal.Current.Returns(Principals.WithClaims(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "jane"), new Claim("client_id", "web")], "Bearer"))));
        _subject = new AvailabilitySubjectProvider(tenant, principal).GetSubject();
    }

    [Fact] void should_pass_no_client_id() => _subject.SystemClientId.ShouldEqual(string.Empty);
    [Fact] void should_keep_the_user() => _subject.User.ShouldEqual("jane");
}
