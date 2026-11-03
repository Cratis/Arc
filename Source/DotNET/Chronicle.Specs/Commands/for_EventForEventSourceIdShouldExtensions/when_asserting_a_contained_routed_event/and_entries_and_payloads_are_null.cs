// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventForEventSourceIdShouldExtensions.when_asserting_a_contained_routed_event;

public class and_entries_and_payloads_are_null : Specification
{
    Exception _error;
    void Because()
    {
        EventForEventSourceId[] events = [null!, new("first", null!)];
        _error = Catch.Exception(() => events.ShouldContainEvent<given.ItemAdded>("first"));
    }
    [Fact] void should_never_match_null() => _error.ShouldBeOfExactType<CommandDecisionAssertionException>();
    [Fact] void should_list_the_null_entry() => _error.Message.ShouldContain("[0] <null>");
    [Fact] void should_list_the_null_payload() => _error.Message.ShouldContain("[1] first: <null>");
}
