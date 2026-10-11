// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Reactors.for_EventsAndCommandsResultHandler.when_handling;

public class with_bare_events : given.a_mixed_result_handler
{
    async Task Because() => _result = await _dispatcher.Handle(_context, _store, new object[] { new TestEvent("bare"), new TestCommand("next") });

    [Fact] void should_append_to_the_observed_source_without_an_override() => _appended.Single().EventSourceId.ShouldEqual((EventSourceId)"observed");
    [Fact] void should_append_before_executing_commands() => _order.SequenceEqual(["append", "commands"]).ShouldBeTrue();
}
