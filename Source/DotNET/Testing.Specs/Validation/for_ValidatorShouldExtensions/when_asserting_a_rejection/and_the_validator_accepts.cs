// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Validation.for_ValidatorShouldExtensions.when_asserting_a_rejection;

public class and_the_validator_accepts : Specification
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => new given.an_item_validator().ShouldReject(new given.AddItem("item"), "Name is required"));
    [Fact] void should_fail() => _error.ShouldBeOfExactType<ValidatorAssertionException>();
    [Fact] void should_say_it_was_accepted() => _error.Message.ShouldContain("accepted");
}
