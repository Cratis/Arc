// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventForEventSourceIdShouldExtensions.when_asserting_a_routed_event;

public class and_the_event_matches : Specification
{
    given.ItemAdded _event = new(3);
    given.ItemAdded _matched;
    void Because() => _matched = new EventForEventSourceId("first", _event).ShouldBeEvent<given.ItemAdded>(_ => _.Quantity == 3);
    [Fact] void should_assert_the_payload() => _matched.ShouldEqual(_event);
}
