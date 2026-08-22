// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Arc.Chronicle.for_CommandScenario_with_UniqueConstraint.given;

public class a_command_that_appends_an_event_with_a_unique_property : Specification
{
    protected CommandScenario<ACommandWithUniquePropertyEvent> _scenario;
    protected EventSourceId _firstEventSourceId;
    protected EventSourceId _secondEventSourceId;

    [EventType]
    public record AnEventWithUniqueProperty([property: Unique("unique-name-spec-constraint")] string Name);

    [Command]
    public class ACommandWithUniquePropertyEvent
    {
        public EventSourceId EventSourceId { get; init; } = EventSourceId.Unspecified;
        public string Name { get; init; } = string.Empty;

        AnEventWithUniqueProperty Handle() => new(Name);
    }

    async Task Establish()
    {
        _firstEventSourceId = EventSourceId.New();
        _secondEventSourceId = EventSourceId.New();
        _scenario = new CommandScenario<ACommandWithUniquePropertyEvent>();
        await _scenario.Execute(new ACommandWithUniquePropertyEvent { EventSourceId = _firstEventSourceId, Name = "Alice" });
    }
}
