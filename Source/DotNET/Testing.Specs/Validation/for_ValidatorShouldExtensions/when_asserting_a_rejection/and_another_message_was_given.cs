// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Validation.for_ValidatorShouldExtensions.when_asserting_a_rejection;

public class and_another_message_was_given : Specification
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => new given.an_item_validator().ShouldReject(new given.AddItem(""), "Quantity must be positive"));
    [Fact] void should_fail() => _error.ShouldBeOfExactType<ValidatorAssertionException>();
    [Fact] void should_show_the_actual_message() => _error.Message.ShouldContain("no failure said 'Quantity must be positive'. Actual: Name: 'Name is required' (Error)");
}
