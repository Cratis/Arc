// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Monads;
using Catch = Cratis.Specifications.Catch;

namespace Cratis.Arc.Testing.Commands.for_CommandRejectionShouldExtensions.when_asserting_acceptance;

public class and_it_was_rejected : Specification
{
    Exception _error;
    void Because()
    {
        Result<string, ValidationResult> result = ValidationResult.Error("Unavailable");
        _error = Catch.Exception(() => result.ShouldBeAccepted());
    }
    [Fact] void should_fail() => _error.ShouldBeOfExactType<CommandDecisionAssertionException>();
    [Fact] void should_describe_the_rejection() => _error.Message.ShouldContain("Error rule/<no detail> ('Unavailable')");
}
