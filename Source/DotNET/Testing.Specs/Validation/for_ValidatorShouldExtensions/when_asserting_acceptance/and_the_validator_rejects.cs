// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Validation.for_ValidatorShouldExtensions.when_asserting_acceptance;

public class and_the_validator_rejects : Specification
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => new given.an_item_validator().ShouldAccept(new given.AddItem("", 0)));
    [Fact] void should_fail() => _error.ShouldBeOfExactType<ValidatorAssertionException>();
    [Fact] void should_show_the_error() => _error.Message.ShouldContain("Name: 'Name is required' (Error)");
    [Fact] void should_show_the_warning() => _error.Message.ShouldContain("Quantity: 'Quantity must be positive' (Warning)");
}
