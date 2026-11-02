// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventForEventSourceIdShouldExtensions.when_asserting_a_contained_routed_event;

public class and_the_payload_has_the_wrong_type : Specification
{
    Exception _error;
    void Because()
    {
        EventForEventSourceId[] events = [new("first", new given.ItemRemoved(2))];
        _error = Catch.Exception(() => events.ShouldContainEvent<given.ItemAdded>("first"));
    }
    [Fact] void should_require_the_payload_type_as_well_as_the_source() => _error.ShouldBeOfExactType<CommandDecisionAssertionException>();
    [Fact] void should_describe_the_actual_payload() => _error.Message.ShouldContain("first: ItemRemoved { Quantity = 2 }");
}
