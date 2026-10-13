// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_appending_to_two_sources;

public class with_one_occupied_source : given.two_generated_sources
{
    bool _secondHasEvents;
    EventSequenceNumber _tail;

    void Establish() => _scenario.Given.ForEventSource(_first).Events(new given.a_fresh_identifier_scenario.Created("existing"));

    async Task Because()
    {
        _result = await _scenario.Execute(_command);
        _tail = await _scenario.EventLog.GetTailSequenceNumber();
        _secondHasEvents = await _scenario.EventLog.HasEventsFor(_second);
    }

    [Fact] void should_reject_the_collision() => _result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_identify_generated_provenance() => _result.ValidationResults.Single().ReasonDetail.ShouldEqual("GeneratedIdentifierCollision");
    [Fact] void should_roll_back_all_source_batches() => _tail.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_not_create_the_other_source() => _secondHasEvents.ShouldBeFalse();
    [Fact] void should_invoke_handle_once() => _command.Calls.ShouldEqual(1);
}
