// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.for_AudienceResolver;

public class when_the_environment_switch_is_on_in_a_test_environment_with_an_undeclared_grant : Specification
{
    InMemoryAudienceSource _source;
    EffectiveAudiences _result;

    void Establish() => _source = new();

    async Task Because()
    {
        await _source.Grant(AudienceLevel.Tenant, "t1", "Ghost");
        _result = await new AudienceResolver(
            _source,
            new DeclaredAudiences("Plus"),
            new AvailabilityOptions { EnvironmentName = "Test", GrantAllDeclaredAudiences = true })
            .Resolve(new AudienceSubject("t1", null));
    }

    [Fact] void should_still_not_match_the_undeclared_audience() => _result.Holds("Ghost").ShouldBeFalse();
    [Fact] void should_still_report_it() => _result.Undeclared.Count.ShouldEqual(1);
}
