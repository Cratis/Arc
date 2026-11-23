// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.Validation;

namespace Cratis.Arc.Testing.Commands.for_CommandDecisionValues;

public class when_extracting_validation_from_a_native_union : Specification
{
    ValidationResult _rejection = ValidationResult.Error("Unavailable", reason: ValidationResultReason.DependencyUnavailable);
    ValidationResult _matched;

    void Because()
    {
        Outcome result = _rejection;
        _matched = (ValidationResult)CommandDecisionValues.Unwrap(result)!;
    }

    [Fact] void should_unwrap_the_active_rejection_case() => _matched.ShouldEqual(_rejection);

    // Compiler-generated union struct; equality is not meaningful for a handler return.
#pragma warning disable CA1815
    public readonly union Outcome(string, ValidationResult);
#pragma warning restore CA1815
}
#endif
