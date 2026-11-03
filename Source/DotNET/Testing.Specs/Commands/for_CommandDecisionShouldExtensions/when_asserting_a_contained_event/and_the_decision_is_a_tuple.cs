// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_CommandDecisionShouldExtensions.when_asserting_a_contained_event;

public class and_the_decision_is_a_tuple : Specification
{
    given.ItemAdded _event = new(3);
    given.ItemAdded _matched;
    void Because() => _matched = (Guid.Empty, (new given.ItemRemoved(2), new[] { _event })).ShouldContainEvent<given.ItemAdded>();
    [Fact] void should_search_nested_tuples_and_collections() => _matched.ShouldEqual(_event);
}
