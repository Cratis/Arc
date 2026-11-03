// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_CommandDecisionShouldExtensions.when_asserting_a_contained_assignable_event;

public class and_a_derived_event_is_present : Specification
{
    given.SpecialItemAdded _event = new(3);
    given.ItemAdded _matched;
    void Because() => _matched = new[] { _event }.ShouldContainEventAssignableTo<given.ItemAdded>();
    [Fact] void should_return_the_derived_event() => _matched.ShouldEqual(_event);
}
