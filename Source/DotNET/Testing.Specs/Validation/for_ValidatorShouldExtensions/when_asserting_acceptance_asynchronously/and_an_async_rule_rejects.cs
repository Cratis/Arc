// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Validation.for_ValidatorShouldExtensions.when_asserting_acceptance_asynchronously;

public class and_an_async_rule_rejects : Specification
{
    Exception _error;
    async Task Because() => _error = await Catch.Exception(() => new given.an_asynchronous_item_validator().ShouldAcceptAsync(new given.AddItem("reserved")));
    [Fact] void should_fail() => _error.ShouldBeOfExactType<ValidatorAssertionException>();
    [Fact] void should_show_the_rule_message() => _error.Message.ShouldContain("Name is reserved");
}
