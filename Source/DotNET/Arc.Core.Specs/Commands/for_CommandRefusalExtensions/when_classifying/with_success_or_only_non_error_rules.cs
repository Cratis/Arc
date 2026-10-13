// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandRefusalExtensions.when_classifying;

public class with_success_or_only_non_error_rules : Specification
{
    CommandRefusal?[] _refusals;

    void Because() => _refusals = new CommandResult[]
    {
        new(),
        new() { AuthorizationFailureReason = "opaque policy unsupported" },
        new() { ValidationResults = [ValidationResult.Warning("warning"), ValidationResult.Information("information")] }
    }.Select(result => result.GetRefusal()).ToArray();

    [Fact] void should_not_classify_success_or_non_error_rules_as_refusals() => _refusals.All(refusal => refusal is null).ShouldBeTrue();
}
