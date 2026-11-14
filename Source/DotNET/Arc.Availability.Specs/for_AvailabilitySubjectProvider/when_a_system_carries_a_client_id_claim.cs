// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Tenancy;
using Cratis.Choreography.Availability.Audiences;

namespace Cratis.Arc.Availability.for_AvailabilitySubjectProvider;

public class when_a_system_carries_a_client_id_claim : Specification
{
    AudienceSubject _subject;

    void Because()
    {
        var tenant = Substitute.For<ITenantIdAccessor>();
        tenant.Current.Returns(TenantId.NotSet);
        var principal = Substitute.For<ICurrentPrincipalAccessor>();
        principal.Current.Returns(Principals.WithClaims(SystemPrincipal.WithRoles(), new Claim("client_id", "svc-a")));
        _subject = new AvailabilitySubjectProvider(tenant, principal).GetSubject();
    }

    [Fact] void should_pass_the_client_id() => _subject.SystemClientId.ShouldEqual("svc-a");
    [Fact] void should_be_system() => _subject.IsSystem.ShouldBeTrue();
}
