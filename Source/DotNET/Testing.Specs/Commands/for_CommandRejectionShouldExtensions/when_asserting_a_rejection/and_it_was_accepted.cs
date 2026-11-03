// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Monads;
using Catch = Cratis.Specifications.Catch;

namespace Cratis.Arc.Testing.Commands.for_CommandRejectionShouldExtensions.when_asserting_a_rejection;

public class and_it_was_accepted : Specification
{
    Exception _error;
    void Because()
    {
        Result<string, ValidationResult> result = "accepted";
        _error = Catch.Exception(() => result.ShouldBeRejected());
    }
    [Fact] void should_fail() => _error.ShouldBeOfExactType<CommandDecisionAssertionException>();
    [Fact] void should_show_the_success_value() => _error.Message.ShouldContain("String (accepted)");
}
