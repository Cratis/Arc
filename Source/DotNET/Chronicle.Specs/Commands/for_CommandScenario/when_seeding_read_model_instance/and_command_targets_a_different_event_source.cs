// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_seeding_read_model_instance;

public class and_command_targets_a_different_event_source : Specification
{
    CommandScenario<UseNullableReducerReadModelInHandle> _scenario;
    EventSourceId _seededEventSourceId;
    EventSourceId _commandEventSourceId;

    void Establish()
    {
        _seededEventSourceId = EventSourceId.New();
        _commandEventSourceId = EventSourceId.New();
        _scenario = new CommandScenario<UseNullableReducerReadModelInHandle>();
        _scenario.Given.ForEventSource(_seededEventSourceId).ReadModel(new ReducerAccountSummary(100m, false));
    }

    async Task Because() => await _scenario.Execute(new UseNullableReducerReadModelInHandle(_commandEventSourceId));

    [Fact] async Task should_inject_null_because_nothing_is_seeded_for_the_command_event_source() =>
        await _scenario.ShouldHaveAppendedEvent<UseNullableReducerReadModelInHandle, ReducerReadModelAbsence>(_commandEventSourceId, @event => @event.WasAbsent);
}
