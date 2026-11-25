// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.for_AudienceResolver;

public class when_a_level_withdraws_an_audience_another_level_still_grants : Specification
{
    InMemoryAudienceSource _source;
    EffectiveAudiences _result;

    void Establish() => _source = new();

    async Task Because()
    {
        await _source.Grant(AudienceLevel.Tenant, "t1", "Plus");
        await _source.Grant(AudienceLevel.User, "u1", "Plus");
        await _source.Withdraw(AudienceLevel.Tenant, "t1", "Plus");
        _result = await new AudienceResolver(_source, new DeclaredAudiences("Plus"), new AvailabilityOptions())
            .Resolve(new AudienceSubject("t1", "u1"));
    }

    [Fact] void should_still_hold_it_through_the_other_level() => _result.Holds("Plus").ShouldBeTrue();
}
