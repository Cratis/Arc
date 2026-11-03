// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Monads;
using Catch = Cratis.Specifications.Catch;

namespace Cratis.Arc.Testing.Commands.for_CommandDecisionShouldExtensions.when_asserting_an_event;

public class and_the_decision_is_a_rejected_result : Specification
{
    Exception _error;
    void Because()
    {
        Result<given.ItemAdded, ValidationResult> result = ValidationResult.Error("Unavailable", reasonDetail: "stock");
        _error = Catch.Exception(() => result.ShouldBeEvent<given.ItemAdded>());
    }
    [Fact] void should_fail() => _error.ShouldBeOfExactType<CommandDecisionAssertionException>();
    [Fact] void should_describe_the_rejection() => _error.Message.ShouldContain("rejected: Error rule/stock ('Unavailable')");
}
