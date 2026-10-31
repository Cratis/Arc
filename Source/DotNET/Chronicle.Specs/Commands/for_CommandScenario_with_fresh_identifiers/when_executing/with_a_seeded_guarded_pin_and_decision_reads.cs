// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;

using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.a_fresh_identifier_scenario;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_executing;

public class with_a_seeded_guarded_pin_and_decision_reads : given.a_default_mode_scenario
{
    EventSequenceNumber _tailBefore;
    EventSequenceNumber _tailAfter;

    async Task Establish()
    {
        _scenario.Given.ForEventSource(_id).Events(new Created("existing"));
        _tailBefore = await _scenario.EventLog.GetTailSequenceNumber();
    }

    async Task Because()
    {
        _error = await Catch.Exception(async () => _result = await _scenario.Execute(_command));
        _tailAfter = await _scenario.EventLog.GetTailSequenceNumber();
    }

    [Fact] void should_not_refuse_the_scenario() => _error.ShouldBeNull();
    [Fact] void should_reject_the_collision() => _result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_name_the_collision() => _result.ValidationResults.Single().ReasonDetail.ShouldEqual("GeneratedIdentifierCollision");
    [Fact] void should_append_nothing() => _tailAfter.ShouldEqual(_tailBefore);
    [Fact] void should_handle_once() => _command.Calls.ShouldEqual(1);
}
