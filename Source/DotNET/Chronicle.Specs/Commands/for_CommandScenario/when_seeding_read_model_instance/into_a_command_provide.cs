// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_seeding_read_model_instance;

public class into_a_command_provide : Specification
{
    CommandScenario<ProvideReadModelDependencyCommand> _scenario;
    EventSourceId _eventSourceId;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _scenario = new CommandScenario<ProvideReadModelDependencyCommand>();
        _scenario.Given.ForEventSource(_eventSourceId).ReadModel(new AccountBalanceReadModel(42m));
    }

    async Task Because() => await _scenario.Execute(new ProvideReadModelDependencyCommand(_eventSourceId));

    [Fact] async Task should_inject_the_seeded_read_model_into_provide() =>
        await _scenario.ShouldHaveAppendedEvent<ProvideReadModelDependencyCommand, ReadModelDependencyProvided>(_eventSourceId, @event => @event.Balance == 42m);
}
