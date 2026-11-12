// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Tenancy;
using Cratis.Choreography.Availability.Audiences;

namespace Cratis.Arc.Availability.for_AvailabilitySubjectProvider;

public class when_the_system_executes : Specification
{
    AudienceSubject _subject;

    void Because()
    {
        var tenant = Substitute.For<ITenantIdAccessor>();
        tenant.Current.Returns(TenantId.NotSet);
        var principal = Substitute.For<ICurrentPrincipalAccessor>();
        principal.Current.Returns(SystemPrincipal.WithRoles());
        _subject = new AvailabilitySubjectProvider(tenant, principal).GetSubject();
    }

    [Fact] void should_be_system() => _subject.IsSystem.ShouldBeTrue();
    [Fact] void should_have_no_user() => _subject.User.ShouldBeNull();
    [Fact] void should_have_no_tenant() => _subject.Tenant.ShouldBeNull();
}
