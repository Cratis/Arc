// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Arc.Chronicle.Commands.for_AggregateRootCommitResultExtensions.when_converting;

public class a_result_with_constraint_violations : given.all_dependencies
{
    AggregateRootCommitResult _commitResult;
    CommandResult _result;
    ConstraintViolation _violation;

    void Establish()
    {
        _violation = new ConstraintViolation(
            EventTypeId.Unknown,
            EventSequenceNumber.Unavailable,
            ConstraintType.Unknown,
            new ConstraintName("TestConstraint"),
            new ConstraintViolationMessage("Constraint was violated"),
            new ConstraintViolationDetails());

        _commitResult = new AggregateRootCommitResult
        {
            ConstraintViolations = [_violation]
        };
    }

    void Because() => _result = _commitResult.ToCommandResult(_correlationId);

    [Fact] void should_return_failed_command_result() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_correct_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_have_one_validation_result() => _result.ValidationResults.Count().ShouldEqual(1);
    [Fact] void should_include_constraint_violation_message() => _result.ValidationResults.First().Message.ShouldEqual("Constraint was violated");
}
