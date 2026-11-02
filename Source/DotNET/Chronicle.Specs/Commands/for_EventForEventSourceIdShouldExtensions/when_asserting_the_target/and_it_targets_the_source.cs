// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventForEventSourceIdShouldExtensions.when_asserting_the_target;

public class and_it_targets_the_source : Specification
{
    EventForEventSourceId _routed = new("first", new given.ItemAdded(3));
    EventForEventSourceId _matched;
    void Because() => _matched = _routed.ShouldTarget("first");
    [Fact] void should_return_the_routed_event() => _matched.ShouldEqual(_routed);
}
