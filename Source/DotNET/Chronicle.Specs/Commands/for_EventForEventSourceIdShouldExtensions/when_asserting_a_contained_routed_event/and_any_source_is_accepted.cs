// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventForEventSourceIdShouldExtensions.when_asserting_a_contained_routed_event;

public class and_any_source_is_accepted : Specification
{
    given.ItemAdded _matched;
    void Because()
    {
        EventForEventSourceId[] events = [new("first", new given.ItemRemoved(1)), new("second", new given.ItemAdded(2))];
        _matched = events.ShouldContainEvent<given.ItemAdded>();
    }
    [Fact] void should_search_across_sources() => _matched.Quantity.ShouldEqual(2);
}
