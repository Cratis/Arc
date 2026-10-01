// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_ConcurrencyViolationExtensions.when_converting;

public class with_a_no_events_expectation : Specification
{
    EventSourceId _eventSourceId;
    ConcurrencyViolation _violation;
    ValidationResult _result;

    void Establish()
    {
        _eventSourceId = "6f2c3b0e-6f1f-4d7e-9f3e-0a4d2c9b8e71";
        _violation = new(_eventSourceId, EventSequenceNumber.BeforeFirst, 0UL);
    }

    void Because() => _result = _violation.ToValidationResult();

    [Fact] void should_name_the_event_source_and_say_what_to_do() => _result.Message.ShouldEqual(
        "Event source '6f2c3b0e-6f1f-4d7e-9f3e-0a4d2c9b8e71' has new events since the command read it: expected no events, but it has events up to sequence number 0. Read it again and resubmit.");
    [Fact] void should_not_show_the_raw_sentinel() => _result.Message.ShouldNotContain(EventSequenceNumber.BeforeFirst.Value.ToString());
    [Fact] void should_say_the_rejection_is_a_concurrency_violation() => _result.Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_carry_the_violation_as_state() => _result.State.ShouldEqual(_violation);
}
