// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Tenancy;
using Cratis.Choreography.Availability.Audiences;

namespace Cratis.Arc.Availability.for_AvailabilitySubjectProvider;

public class when_a_system_carries_only_an_azp_claim : Specification
{
    AudienceSubject _subject;

    void Because()
    {
        var tenant = Substitute.For<ITenantIdAccessor>();
        tenant.Current.Returns(TenantId.NotSet);
        var principal = Substitute.For<ICurrentPrincipalAccessor>();
        principal.Current.Returns(Principals.WithClaims(SystemPrincipal.WithRoles(), new Claim("azp", "svc-b")));
        _subject = new AvailabilitySubjectProvider(tenant, principal).GetSubject();
    }

    [Fact] void should_pass_the_authorized_party() => _subject.SystemClientId.ShouldEqual("svc-b");
}
