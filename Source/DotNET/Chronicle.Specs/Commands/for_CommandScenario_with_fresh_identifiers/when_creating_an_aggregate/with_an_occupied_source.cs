// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_creating_an_aggregate;

public class with_an_occupied_source : given.a_generated_aggregate_scenario
{
    EventSequenceNumber _tail;

    void Establish() => _scenario.Given.ForEventSource(_id).Events(new given.a_fresh_identifier_scenario.Created("existing"));

    async Task Because()
    {
        _result = await _scenario.Execute(_command);
        _tail = await _scenario.EventLog.GetTailSequenceNumber();
    }

    [Fact] void should_reject_acquisition_as_concurrency() => _result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_identify_the_generated_collision() => _result.ValidationResults.Single().ReasonDetail.ShouldEqual("GeneratedIdentifierCollision");
    [Fact] void should_append_nothing() => _tail.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_invoke_handle_once() => _command.Calls.ShouldEqual(1);
}
