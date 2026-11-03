// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Monads;
using Catch = Cratis.Specifications.Catch;

namespace Cratis.Arc.Testing.Commands.for_CommandRejectionShouldExtensions.when_asserting_a_rejection_reason;

public class and_only_a_substring_matches : Specification
{
    Exception _error;
    void Because()
    {
        Result<string, ValidationResult> result = ValidationResult.Error("Item unavailable");
        _error = Catch.Exception(() => result.ShouldBeRejected("unavailable"));
    }
    [Fact] void should_require_an_exact_message() => _error.ShouldBeOfExactType<CommandDecisionAssertionException>();
    [Fact] void should_show_the_actual_message() => _error.Message.ShouldContain("Item unavailable");
}
