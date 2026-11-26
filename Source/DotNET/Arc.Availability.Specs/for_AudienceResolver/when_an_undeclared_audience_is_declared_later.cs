// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.for_AudienceResolver;

public class when_an_undeclared_audience_is_declared_later : Specification
{
    InMemoryAudienceSource _source;
    EffectiveAudiences _result;

    void Establish() => _source = new();

    async Task Because()
    {
        await _source.Grant(AudienceLevel.Tenant, "t1", "Ghost");
        _result = await new AudienceResolver(_source, new DeclaredAudiences("Ghost"), new AvailabilityOptions())
            .Resolve(new AudienceSubject("t1", null));
    }

    [Fact] void should_make_the_old_grant_live() => _result.Holds("Ghost").ShouldBeTrue();
}
