// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.for_AudienceResolver;

public class when_resolving_the_union_of_all_levels : Specification
{
    InMemoryAudienceSource _source;
    EffectiveAudiences _result;

    void Establish() => _source = new();

    async Task Because()
    {
        await _source.Grant(AudienceLevel.Tenant, "t1", "Plus");
        await _source.Grant(AudienceLevel.User, "u1", "Preview");
        await _source.Grant(AudienceLevel.System, null, "Internal");
        await _source.Grant(AudienceLevel.Tenant, "t2", "Other");
        _result = await new AudienceResolver(
            _source,
            new DeclaredAudiences("Plus", "Preview", "Internal", "Other"),
            new AvailabilityOptions()).Resolve(new AudienceSubject("t1", "u1", IsSystem: true));
    }

    [Fact] void should_hold_the_tenant_audience() => _result.Holds("Plus").ShouldBeTrue();
    [Fact] void should_hold_the_user_audience() => _result.Holds("Preview").ShouldBeTrue();
    [Fact] void should_hold_the_system_audience() => _result.Holds("Internal").ShouldBeTrue();
    [Fact] void should_not_hold_another_tenants_audience() => _result.Holds("Other").ShouldBeFalse();
}
