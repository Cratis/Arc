// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandRefusalExtensions.when_classifying;

public class with_constraints_and_validation : given.a_command_result
{
    void Establish() => _result.ValidationResults =
    [
        ValidationResult.Error("rule"),
        ValidationResult.Error("$strings.unique", reason: ValidationResultReason.ConstraintViolation, reasonDetail: "UniqueSeat"),
        ValidationResult.Error("another", reason: ValidationResultReason.ConstraintViolation, reasonDetail: "Other"),
        ValidationResult.Error(" detail ", reason: ValidationResultReason.ConstraintViolation, reasonDetail: "UniqueSeat")
    ];
    void Because() => _refusal = _result.GetRefusal();

    [Fact] void should_prefer_the_first_constraint() => _refusal!.Kind.ShouldEqual(CommandRefusalKind.Constraint);
    [Fact] void should_preserve_the_constraint_name() => _refusal!.ConstraintName.ShouldEqual("UniqueSeat");
    [Fact] void should_join_only_messages_for_that_constraint() => _refusal!.Message.ShouldEqual("$strings.unique\n detail ");
}
