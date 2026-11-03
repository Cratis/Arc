// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventForEventSourceIdShouldExtensions.when_asserting_a_routed_event;

public class and_the_predicate_does_not_match : Specification
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => new EventForEventSourceId("first", new given.ItemAdded(3)).ShouldBeEvent<given.ItemAdded>(_ => _.Quantity == 4));
    [Fact] void should_fail() => _error.ShouldBeOfExactType<CommandDecisionAssertionException>();
    [Fact] void should_show_the_original_predicate() => _error.Message.ShouldContain("_ => _.Quantity == 4");
    [Fact] void should_show_the_payload() => _error.Message.ShouldContain("ItemAdded { Quantity = 3 }");
}
