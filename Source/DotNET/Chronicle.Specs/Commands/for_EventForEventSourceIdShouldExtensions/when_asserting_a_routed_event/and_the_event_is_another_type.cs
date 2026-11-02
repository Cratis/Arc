// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventForEventSourceIdShouldExtensions.when_asserting_a_routed_event;

public class and_the_event_is_another_type : Specification
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => new EventForEventSourceId("first", new given.ItemRemoved(3)).ShouldBeEvent<given.ItemAdded>());
    [Fact] void should_fail() => _error.ShouldBeOfExactType<CommandDecisionAssertionException>();
    [Fact] void should_show_the_payload() => _error.Message.ShouldContain("ItemRemoved { Quantity = 3 }");
}
