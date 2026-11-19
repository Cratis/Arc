// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Tenancy;
using Cratis.Choreography.Availability.Audiences;

namespace Cratis.Arc.Availability.for_AvailabilitySubjectProvider;

public class when_the_request_has_a_tenant_and_a_user : Specification
{
    AudienceSubject _subject;

    void Because()
    {
        var tenant = Substitute.For<ITenantIdAccessor>();
        tenant.Current.Returns(new TenantId("acme"));
        var principal = Substitute.For<ICurrentPrincipalAccessor>();
        principal.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "alice")], "test")));
        _subject = new AvailabilitySubjectProvider(tenant, principal).GetSubject();
    }

    [Fact] void should_use_the_tenant() => _subject.Tenant.ShouldEqual("acme");
    [Fact] void should_use_the_subject_of_the_principal() => _subject.User.ShouldEqual("alice");
    [Fact] void should_not_be_system() => _subject.IsSystem.ShouldBeFalse();
}
