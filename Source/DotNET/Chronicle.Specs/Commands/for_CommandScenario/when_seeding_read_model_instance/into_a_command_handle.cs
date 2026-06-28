// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_seeding_read_model_instance;

public class into_a_command_handle : Specification
{
    CommandScenario<UseReadModelDependencyCommand> _scenario;
    EventSourceId _eventSourceId;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _scenario = new CommandScenario<UseReadModelDependencyCommand>();
        _scenario.Given.ForEventSource(_eventSourceId).ReadModel(new AccountBalanceReadModel(42m));
    }

    async Task Because() => await _scenario.Execute(new UseReadModelDependencyCommand(_eventSourceId));

    [Fact] async Task should_inject_the_seeded_read_model_into_the_handler() =>
        await _scenario.ShouldHaveAppendedEvent<UseReadModelDependencyCommand, ReadModelDependencyUsed>(_eventSourceId, @event => @event.Balance == 42m);
}
