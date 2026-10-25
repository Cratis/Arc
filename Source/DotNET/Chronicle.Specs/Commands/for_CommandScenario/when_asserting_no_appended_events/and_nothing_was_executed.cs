// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_asserting_no_appended_events;

public class and_nothing_was_executed : given.an_appended_event_assertion
{
    async Task Because() => _error = await Catch.Exception(_scenario.ShouldNotHaveAppendedEvents);

    [Fact] void should_fail() => _error.ShouldBeOfExactType<CommandResultAssertionException>();
    [Fact] void should_explain_the_required_execution() => _error.Message.ShouldEqual("Execute a command before asserting appended events.");
    [Fact] async Task should_require_execution_for_a_source() => (await Catch.Exception(() => _scenario.ShouldNotHaveAppendedEvents(_source))).ShouldBeOfExactType<CommandResultAssertionException>();
    [Fact] async Task should_require_execution_for_an_empty_ordered_list() => (await Catch.Exception(() => _scenario.ShouldHaveAppendedEventsInOrder())).ShouldBeOfExactType<CommandResultAssertionException>();
    [Fact] async Task should_require_execution_for_an_empty_unordered_list() => (await Catch.Exception(() => _scenario.ShouldHaveAppendedEventsInAnyOrder())).ShouldBeOfExactType<CommandResultAssertionException>();
}
