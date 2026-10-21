// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandRefusalExtensions.when_classifying;

public class with_authorization_and_validation : given.a_command_result
{
    void Establish()
    {
        _result = CommandResult.Unauthorized(CorrelationId.NotSet, " $strings.denied ");
        _result.ValidationResults = [ValidationResult.Error("rule"), ValidationResult.Error("unique", reason: ValidationResultReason.ConstraintViolation, reasonDetail: "UniqueSeat")];
    }
    void Because() => _refusal = _result.GetRefusal();

    [Fact] void should_prefer_authorization() => _refusal!.Kind.ShouldEqual(CommandRefusalKind.Authorization);
    [Fact] void should_preserve_the_reason_verbatim() => _refusal!.Message.ShouldEqual(" $strings.denied ");
    [Fact] void should_not_supply_a_constraint_name() => _refusal!.ConstraintName.ShouldBeNull();
}
