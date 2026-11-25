// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.for_AudienceResolver;

public class when_the_system_level_applies_only_to_the_system_caller : Specification
{
    InMemoryAudienceSource _source;
    EffectiveAudiences _result;

    void Establish() => _source = new();

    async Task Because()
    {
        await _source.Grant(AudienceLevel.System, null, "Internal");
        _result = await new AudienceResolver(_source, new DeclaredAudiences("Internal"), new AvailabilityOptions())
            .Resolve(new AudienceSubject("t1", "u1"));
    }

    [Fact] void should_not_hold_it() => _result.Holds("Internal").ShouldBeFalse();
}
