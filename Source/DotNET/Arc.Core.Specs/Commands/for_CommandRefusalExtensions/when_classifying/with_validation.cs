// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandRefusalExtensions.when_classifying;

public class with_validation : given.a_command_result
{
    void Establish() => _result.ValidationResults = [ValidationResult.Error("$strings.first"), ValidationResult.Warning("ignored"), ValidationResult.Error(" second ")];
    void Because() => _refusal = _result.GetRefusal();

    [Fact] void should_classify_validation() => _refusal!.Kind.ShouldEqual(CommandRefusalKind.Validation);
    [Fact] void should_preserve_error_messages_in_order() => _refusal!.Message.ShouldEqual("$strings.first\n second ");
    [Fact] void should_not_supply_a_constraint_name() => _refusal!.ConstraintName.ShouldBeNull();
}
