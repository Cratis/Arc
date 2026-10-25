// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.given;

public class an_appended_event_assertion : Specification
{
    protected CommandScenario<AppendExpectedEvents> _scenario;
    protected EventSourceId _source;
    protected CommandResult _result;
    protected Exception _error;

    void Establish()
    {
        _scenario = new();
        _source = EventSourceId.New();
    }

    async Task Destroy() => await _scenario.DisposeAsync();

    protected static AssertionEvent Event(string name = "first", string city = "Oslo", params string[] tags) =>
        new(name, new AssertionAddress { City = city }, tags);

    [Command]
    public record AppendExpectedEvents(EventSourceId Source, IEnumerable<AssertionEvent> Events, bool IncludeOther = false)
    {
        public IEnumerable<EventForEventSourceId> Handle() =>
            Events.Select(@event => new EventForEventSourceId(Source, @event))
                .Concat(IncludeOther ? [new EventForEventSourceId(Source, new OtherAssertionEvent("other"))] : []);
    }

    [EventType]
    public record AssertionEvent(string Name, AssertionAddress Address, IEnumerable<string> Tags);

    [EventType]
    public record OtherAssertionEvent(string Name);

    public class AssertionAddress
    {
        public string City { get; init; } = string.Empty;
    }
}
