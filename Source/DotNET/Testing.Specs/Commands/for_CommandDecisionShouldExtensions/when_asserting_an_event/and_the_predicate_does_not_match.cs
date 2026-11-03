// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_CommandDecisionShouldExtensions.when_asserting_an_event;

public class and_the_predicate_does_not_match : Specification
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => new given.ItemAdded(3).ShouldBeEvent<given.ItemAdded>(_ => _.Quantity == 4));
    [Fact] void should_fail() => _error.ShouldBeOfExactType<CommandDecisionAssertionException>();
    [Fact] void should_show_the_predicate() => _error.Message.ShouldContain("_ => _.Quantity == 4");
    [Fact] void should_show_the_event() => _error.Message.ShouldContain("ItemAdded { Quantity = 3 }");
}
