// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventForEventSourceIdShouldExtensions.when_asserting_a_routed_event;

public class and_the_event_is_derived : Specification
{
    given.SpecialItemAdded _event = new(3);
    given.ItemAdded _matched;
    given.ItemAdded _contained;
    Exception _error;
    void Because()
    {
        var routed = new EventForEventSourceId("first", _event);
        _error = Catch.Exception(() => routed.ShouldBeEvent<given.ItemAdded>());
        _matched = routed.ShouldBeEventAssignableTo<given.ItemAdded>();
        _contained = new[] { routed }.ShouldContainEventAssignableTo<given.ItemAdded>("first");
    }
    [Fact] void should_require_an_exact_type_by_default() => _error.ShouldBeOfExactType<CommandDecisionAssertionException>();
    [Fact] void should_allow_assignable_matching_explicitly() => _matched.ShouldEqual(_event);
    [Fact] void should_allow_assignable_containment_at_the_source() => _contained.ShouldEqual(_event);
}
