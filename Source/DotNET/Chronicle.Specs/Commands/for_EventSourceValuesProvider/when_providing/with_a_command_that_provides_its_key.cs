// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_EventSourceValuesProvider.when_providing;

/// <summary>
/// The ordinary path: a command that composes its key successfully keeps providing exactly that key.
/// </summary>
public class with_a_command_that_provides_its_key : Specification
{
    const string TheKey = "d4d1a3f0-0f4a-4f6a-9a9f-8b2c1e5a7d31";

    EventSourceValuesProvider _provider;
    CommandContextValues _result;

    void Establish() => _provider = new EventSourceValuesProvider();

    void Because() => _result = _provider.Provide(new CommandThatProvidesItsKey());

    [Fact] void should_provide_the_event_source_id_the_command_composed() =>
        ((EventSourceId)_result[WellKnownCommandContextKeys.EventSourceId]).Value.ShouldEqual(TheKey);

    record CommandThatProvidesItsKey : ICanProvideEventSourceId
    {
        public EventSourceId GetEventSourceId() => TheKey;
    }
}
