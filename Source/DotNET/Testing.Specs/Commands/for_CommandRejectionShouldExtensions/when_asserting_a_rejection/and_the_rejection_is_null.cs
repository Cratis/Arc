// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Monads;
using Catch = Cratis.Specifications.Catch;

namespace Cratis.Arc.Testing.Commands.for_CommandRejectionShouldExtensions.when_asserting_a_rejection;

public class and_the_rejection_is_null : Specification
{
    Exception _error;
    void Because()
    {
        var result = Result<string, ValidationResult>.Failed(null!);
        _error = Catch.Exception(() => result.ShouldBeRejected());
    }
    [Fact] void should_fail() => _error.ShouldBeOfExactType<CommandDecisionAssertionException>();
    [Fact] void should_identify_null() => _error.Message.ShouldContain("null");
}
