// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_CommandDecisionShouldExtensions.when_asserting_a_contained_event;

public class and_no_event_matches_the_predicate : Specification
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => new[] { new given.ItemAdded(3), new given.ItemAdded(4) }.ShouldContainEvent<given.ItemAdded>(_ => _.Quantity == 5));
    [Fact] void should_fail() => _error.ShouldBeOfExactType<CommandDecisionAssertionException>();
    [Fact] void should_show_the_predicate() => _error.Message.ShouldContain("_ => _.Quantity == 5");
    [Fact] void should_list_the_first_candidate() => _error.Message.ShouldContain("[0] ItemAdded { Quantity = 3 }");
    [Fact] void should_list_the_second_candidate() => _error.Message.ShouldContain("[1] ItemAdded { Quantity = 4 }");
}
