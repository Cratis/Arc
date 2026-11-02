// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventForEventSourceIdShouldExtensions.when_asserting_a_contained_routed_event;

public class and_the_collection_is_empty : Specification
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => Array.Empty<EventForEventSourceId>().ShouldContainEvent<given.ItemAdded>("first"));
    [Fact] void should_fail() => _error.ShouldBeOfExactType<CommandDecisionAssertionException>();
    [Fact] void should_describe_the_empty_collection() => _error.Message.ShouldContain("no values");
}
