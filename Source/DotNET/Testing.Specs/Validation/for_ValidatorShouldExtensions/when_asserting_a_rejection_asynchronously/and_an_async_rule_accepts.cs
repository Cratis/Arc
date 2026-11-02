// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Validation.for_ValidatorShouldExtensions.when_asserting_a_rejection_asynchronously;

public class and_an_async_rule_accepts : Specification
{
    Exception _error;
    async Task Because() => _error = await Catch.Exception(() => new given.an_asynchronous_item_validator().ShouldRejectAsync(new given.AddItem("item"), "Name is reserved"));
    [Fact] void should_fail() => _error.ShouldBeOfExactType<ValidatorAssertionException>();
    [Fact] void should_say_it_was_accepted() => _error.Message.ShouldContain("accepted");
}
