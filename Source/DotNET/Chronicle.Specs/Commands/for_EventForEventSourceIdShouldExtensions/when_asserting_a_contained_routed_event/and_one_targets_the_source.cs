// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventForEventSourceIdShouldExtensions.when_asserting_a_contained_routed_event;

public class and_one_targets_the_source : Specification
{
    given.ItemAdded _matched;
    void Because()
    {
        EventForEventSourceId[] events = [new("first", new given.ItemAdded(1)), new("second", new given.ItemAdded(2)), new("second", new given.ItemAdded(3))];
        _matched = events.ShouldContainEvent<given.ItemAdded>("second", _ => _.Quantity > 1);
    }
    [Fact] void should_return_the_first_match_at_the_source() => _matched.Quantity.ShouldEqual(2);
}
