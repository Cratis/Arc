// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Validation.for_ValidatorShouldExtensions.when_asserting_acceptance;

public class and_the_validator_has_async_rules : Specification
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => new given.an_asynchronous_item_validator().ShouldAccept(new given.AddItem("item")));
    [Fact] void should_fail_with_an_assertion_exception() => _error.ShouldBeOfExactType<ValidatorAssertionException>();
    [Fact] void should_name_the_async_helper() => _error.Message.ShouldContain("ShouldAcceptAsync");
}
