// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_creating_an_aggregate;

public class with_a_writer_after_acquisition : given.a_generated_aggregate_scenario
{
    EventSequenceNumber _tail;

    void Establish() => _command = new(true);

    async Task Because()
    {
        _result = await _scenario.Execute(_command);
        _tail = await _scenario.EventLog.GetTailSequenceNumber();
    }

    [Fact] void should_reject_the_aggregate_commit_as_concurrency() => _result.ValidationResults.Select(validation => validation.Reason).Distinct().ShouldContainOnly(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_identify_generated_provenance() => _result.ValidationResults.ShouldContain(validation => validation.ReasonDetail == "GeneratedIdentifierCollision");
    [Fact] void should_persist_only_the_competing_writers_event() => _tail.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_invoke_handle_once() => _command.Calls.ShouldEqual(1);
}
