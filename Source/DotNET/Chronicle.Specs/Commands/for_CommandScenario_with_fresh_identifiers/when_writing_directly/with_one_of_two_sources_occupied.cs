// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;

using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.a_fresh_identifier_scenario;
using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.direct_write_commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_writing_directly;

public class with_one_of_two_sources_occupied : given.a_direct_write_scenario<TwoDirectSources>
{
    void Establish()
    {
        var second = new Identifier(new Guid(0x1e273da1, 0xc661, 0x465a, 0xb6, 0x89, 0xa0, 0x1b, 0x14, 0x44, 0xb3, 0x50));
        _scenario.PinGeneratedValue("second", second);
        _scenario.Given.ForEventSource(second).Events(new Created("existing"));
    }
    async Task Because() => await Execute(new());

    [Fact] void should_reject_the_entire_unit() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_classify_the_collision() => _result.ValidationResults.Single().ReasonDetail.ShouldEqual("GeneratedIdentifierCollision");
    [Fact] void should_not_commit_the_fresh_sources_events() => _after.ShouldEqual(_before);
}
