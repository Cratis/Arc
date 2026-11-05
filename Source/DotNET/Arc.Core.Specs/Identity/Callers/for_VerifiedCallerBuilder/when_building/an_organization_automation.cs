// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers.given;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerBuilder.when_building;

/// <summary>Vector 2.</summary>
public class an_organization_automation : given.a_builder
{
    void Establish()
    {
        _facts = verified_facts.Service();
        _tenant = TenantId.NotSet;
    }
    void Because() => Build();

    [Fact] void should_identify_the_service() => _result.Caller.ServicePrincipal.ShouldEqual(_facts.ServicePrincipal);
    [Fact] void should_keep_explicit_permissions() => _result.Caller.Permissions.ShouldContain("fixture:read");
    [Fact] void should_not_fabricate_a_person() => _result.Caller.Person.ShouldBeNull();
    [Fact] void should_not_fabricate_assurance() => _result.Caller.Assurance.ShouldBeNull();
    [Fact] void should_not_fabricate_an_actor() => _result.Caller.Actor.ShouldBeNull();
}
