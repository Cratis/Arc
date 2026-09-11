// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Arc.Chronicle.for_CommandScenario_with_UniqueConstraint.given;

public class a_command_that_appends_an_event_of_a_unique_event_type : Specification
{
    protected CommandScenario<ACommandWithUniqueEventTypeEvent> _scenario;
    protected EventSourceId _eventSourceId;

    [Unique("unique-event-type-spec-constraint")]
    [EventType]
    public record AUniqueEventType(string Name);

    [Command]
    public class ACommandWithUniqueEventTypeEvent
    {
        public EventSourceId EventSourceId { get; init; } = EventSourceId.Unspecified;
        public string Name { get; init; } = string.Empty;

        AUniqueEventType Handle() => new(Name);
    }

    async Task Establish()
    {
        _eventSourceId = EventSourceId.New();
        _scenario = new CommandScenario<ACommandWithUniqueEventTypeEvent>();
        await _scenario.EventScenario.Given.ForEventSource(_eventSourceId).Events(new AUniqueEventType("Bob"));
    }
}
