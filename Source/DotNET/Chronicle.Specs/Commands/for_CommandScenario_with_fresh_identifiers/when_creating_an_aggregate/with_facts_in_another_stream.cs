// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_creating_an_aggregate;

public class with_facts_in_another_stream : given.a_generated_aggregate_scenario
{
    EventSequenceNumber _tail;

    void Establish() => _scenario.Given.ForEventSource(_id)
        .OnRoute(new EventRoute(new("other-source-kind"), new("other-stream"), new("other-id")))
        .Events(new PreexistingFreshnessFact());

    async Task Because()
    {
        _result = await _scenario.Execute(_command);
        _tail = await _scenario.EventLog.GetTailSequenceNumber();
    }

    [Fact] void should_reject_before_rehydration_even_for_an_unhandled_event_type() => _result.ValidationResults.Single().ReasonDetail.ShouldEqual("GeneratedIdentifierCollision");
    [Fact] void should_not_append_to_another_part_of_the_source() => _tail.ShouldEqual(EventSequenceNumber.First);

    [EventType]
    public record PreexistingFreshnessFact;
}
