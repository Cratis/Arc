// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_CommandDecisionShouldExtensions.when_asserting_a_contained_event;

public class and_the_collection_holds_a_null : Specification
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => new object?[] { null, new given.ItemRemoved(2) }.ShouldContainEvent<given.ItemAdded>());
    [Fact] void should_not_match_null() => _error.ShouldBeOfExactType<CommandDecisionAssertionException>();
    [Fact] void should_list_null() => _error.Message.ShouldContain("[0] <null>");
}
