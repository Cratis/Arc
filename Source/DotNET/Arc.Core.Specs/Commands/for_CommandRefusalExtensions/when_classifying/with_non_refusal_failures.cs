// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandRefusalExtensions.when_classifying;

public class with_non_refusal_failures : Specification
{
    CommandRefusal?[] _refusals;

    void Because()
    {
        ValidationResultReason[] reasons =
        [
            ValidationResultReason.ConcurrencyViolation, ValidationResultReason.ValidatorFailed,
            ValidationResultReason.DependencyUnavailable, ValidationResultReason.MalformedRequest, new("unsupported")
        ];
        _refusals = reasons.Select(reason => new CommandResult
        {
            ValidationResults = [ValidationResult.Error("rule"), ValidationResult.Error("failure", reason: reason)]
        }.GetRefusal()).Append(CommandResult.Error(CorrelationId.NotSet, "exception").GetRefusal()).ToArray();
    }

    [Fact] void should_not_classify_any_non_refusal_failure() => _refusals.All(refusal => refusal is null).ShouldBeTrue();
}
