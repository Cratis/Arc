// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Chronicle.Events;

using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.direct_write_commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_writing_directly;

public class with_facts_in_another_stream : given.a_direct_write_scenario<DirectWrite>
{
    void Establish() => _scenario.Given.ForEventSource(_id).OnRoute(new EventRoute("other-source-kind", "other-stream", "other-id")).Events(new UnrelatedFact());
    async Task Because() => await Execute(new(WriteStyle.AddEvent));

    [Fact] void should_guard_the_whole_source() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_classify_the_collision() => _result.ValidationResults.Single().ReasonDetail.ShouldEqual("GeneratedIdentifierCollision");
    [Fact] void should_keep_only_the_preexisting_fact() => _after.ShouldEqual(_before);

    [EventType]
    public record UnrelatedFact;
}
