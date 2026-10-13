// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandRefusalExtensions.when_classifying;

public class with_non_error_framework_results : Specification
{
    CommandRefusal?[] _refusals;

    void Because()
    {
        ValidationResultReason[] reasons =
        [
            ValidationResultReason.ConcurrencyViolation, ValidationResultReason.ValidatorFailed,
            ValidationResultReason.DependencyUnavailable, ValidationResultReason.MalformedRequest,
            ValidationResultReason.FrameworkFailure, new("unknown")
        ];
        _refusals = reasons.Select(reason => new CommandResult
        {
            ValidationResults =
            [
                ValidationResult.Warning("warning", reason: reason),
                ValidationResult.Information("information", reason: reason),
                ValidationResult.Error("authored")
            ]
        }.GetRefusal()).ToArray();
    }

    [Fact] void should_classify_every_authored_error() => _refusals.Select(refusal => refusal!.Kind).ShouldEqual(Enumerable.Repeat(CommandRefusalKind.Validation, 6));
    [Fact] void should_only_carry_the_error_message() => _refusals.Select(refusal => refusal!.Message).ShouldEqual(Enumerable.Repeat("authored", 6));
}
