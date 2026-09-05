// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_AggregateRootCommitResultExtensions.when_converting;

/// <summary>
/// The aggregate-root path to the same client-visible result. It had no concurrency coverage at all, which is how
/// it came to carry the same flattening as the event-log path without anyone noticing the two had diverged.
/// </summary>
public class a_result_with_concurrency_violations : given.all_dependencies
{
    AggregateRootCommitResult _commitResult;
    CommandResult _result;

    void Establish() => _commitResult = new AggregateRootCommitResult
    {
        ConcurrencyViolations =
        [
            new ConcurrencyViolation(EventSourceId.New(), new EventSequenceNumber(10), new EventSequenceNumber(15))
        ]
    };

    void Because() => _result = _commitResult.ToCommandResult(_correlationId);

    [Fact] void should_return_failed_command_result() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_one_validation_result() => _result.ValidationResults.Count().ShouldEqual(1);
    [Fact] void should_say_the_rejection_is_a_concurrency_violation() => _result.ValidationResults.First().Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
}
