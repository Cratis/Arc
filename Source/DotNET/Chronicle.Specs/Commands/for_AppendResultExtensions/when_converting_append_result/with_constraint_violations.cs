// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_AppendResultExtensions.when_converting_append_result;

public class with_constraint_violations : given.all_dependencies
{
    AppendResult _appendResult;
    CommandResult _result;
    ConstraintViolation _violation;

    void Establish()
    {
        _violation = new ConstraintViolation(
            EventTypeId.Unknown,
            EventSequenceNumber.Unavailable,
            ConstraintType.Unknown,
            new ConstraintName("TestConstraint"),
            new ConstraintViolationMessage("Test violation message"),
            new ConstraintViolationDetails { [WellKnownConstraintDetailKeys.PropertyName] = "OrganizationNumber" });

        _appendResult = AppendResult.Failed(_correlationId, [_violation]);
    }

    void Because() => _result = _appendResult.ToCommandResult();

    [Fact] void should_return_failed_command_result() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_correct_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_have_validation_results() => _result.ValidationResults.ShouldNotBeEmpty();
    [Fact] void should_have_one_validation_result() => _result.ValidationResults.Count().ShouldEqual(1);
    [Fact] void should_include_constraint_violation_message() => _result.ValidationResults.First().Message.ShouldEqual("Test violation message");
    [Fact] void should_attribute_the_violation_to_the_camel_cased_member() => _result.ValidationResults.First().Members.ShouldContain("organizationNumber");
    [Fact] void should_say_the_rejection_is_a_constraint_violation() => _result.ValidationResults.First().Reason.ShouldEqual(Validation.ValidationResultReason.ConstraintViolation);
    [Fact] void should_name_the_constraint_that_rejected_the_command() => _result.ValidationResults.First().ReasonDetail.ShouldEqual("TestConstraint");

    /// <summary>
    /// The parity the name exists for: the assertion a spec writes against a raw append holds unchanged once the
    /// same events go through a command, and the <see cref="ConstraintName"/> concept is what it is written with.
    /// </summary>
    [Fact] void should_satisfy_the_same_assertion_a_raw_append_would() => _result.ShouldHaveConstraintViolationFor(new ConstraintName("TestConstraint"));
}
