// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_CommandDecisionShouldExtensions.when_asserting_a_contained_event;

public class and_it_is_among_others : Specification
{
    given.ItemAdded _event = new(3);
    given.ItemAdded _matched;
    void Because() => _matched = new object[] { new given.ItemRemoved(1), _event }.ShouldContainEvent<given.ItemAdded>();
    [Fact] void should_return_the_event() => _matched.ShouldEqual(_event);
}
