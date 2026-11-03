// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventForEventSourceIdShouldExtensions.when_asserting_a_contained_routed_event;

public class and_it_targets_another_source : Specification
{
    Exception _error;
    void Because()
    {
        EventForEventSourceId[] events = [new("first", new given.ItemAdded(1))];
        _error = Catch.Exception(() => events.ShouldContainEvent<given.ItemAdded>("second"));
    }
    [Fact] void should_fail() => _error.ShouldBeOfExactType<CommandDecisionAssertionException>();
    [Fact] void should_name_the_expected_source() => _error.Message.ShouldContain("second");
    [Fact] void should_list_the_routed_candidate() => _error.Message.ShouldContain("[0] first: ItemAdded { Quantity = 1 }");
}
