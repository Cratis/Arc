// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.for_AudienceResolver;

public class when_the_environment_switch_is_off : Specification
{
    InMemoryAudienceSource _source;
    EffectiveAudiences _result;

    void Establish() => _source = new();

    async Task Because() =>
        _result = await new AudienceResolver(_source, new DeclaredAudiences("Plus"), new AvailabilityOptions { EnvironmentName = "Development" })
            .Resolve(new AudienceSubject("t1", null));

    [Fact] void should_grant_nothing_that_is_not_granted() => _result.Holds("Plus").ShouldBeFalse();
}
