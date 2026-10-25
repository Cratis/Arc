// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using static Cratis.Arc.Chronicle.Commands.for_CommandScenario.given.an_appended_event_assertion;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_asserting_appended_events_in_order;

public class and_decision_reads_are_automatic : Specification
{
    CommandScenario<AutomaticallyProtectedAppend> _scenario;
    EventSourceId _source;
    CommandResult _result;
    Exception _error;

    void Establish()
    {
        _source = EventSourceId.New();
        _scenario = new();
        _scenario.Given.ForEventSource(_source).Events(new OtherAssertionEvent("seed"));
    }

    async Task Because()
    {
        _result = await _scenario.Execute(new(_source));
        _error = await Catch.Exception(() => _scenario.ShouldHaveAppendedEventsInOrder(new OtherAssertionEvent("command")));
    }

    [Fact] void should_have_executed_the_command() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_capture_only_command_events() => _error.ShouldBeNull();

    async Task Destroy() => await _scenario.DisposeAsync();

    [Command, ReadProtection(ReadProtectionMode.On)]
    public record AutomaticallyProtectedAppend(EventSourceId Source)
    {
        public EventForEventSourceId Handle() => new(Source, new OtherAssertionEvent("command"));
    }
}
