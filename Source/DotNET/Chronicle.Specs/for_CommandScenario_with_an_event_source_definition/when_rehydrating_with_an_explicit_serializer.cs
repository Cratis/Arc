// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.for_CommandScenario_with_an_event_source_definition;

public class when_rehydrating_with_an_explicit_serializer : given.an_aggregate_scenario_with_an_explicit_serializer
{
    CommandResult _result;

    async Task Establish() => await SeedEntry();

    async Task Because() => _result = await _aggregateScenario.Execute(new(_command.Id, _command.StreamId, _command.Description));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] async Task should_use_the_explicit_serializer() => await _serializer.Received(1).Deserialize(Arg.Any<AppendedEvent>());
}
