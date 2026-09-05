// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Arc.Chronicle.Commands.for_EventSourceValuesProvider.when_providing;

public class with_a_positional_command_parameter_marked_as_key : Specification
{
    EventSourceValuesProvider _provider;
    CommandContextValues _result;
    Guid _id;

    void Establish()
    {
        _provider = new EventSourceValuesProvider(new RecordingLogger<EventSourceValuesProvider>());
        _id = Guid.NewGuid();
    }

    void Because() => _result = _provider.Provide(new CommandWithKey(_id));

    [Fact] void should_resolve_the_event_source_id_from_the_parameter() =>
        ((EventSourceId)_result[WellKnownCommandContextKeys.EventSourceId]).Value.ShouldEqual(_id.ToString());

    [Fact] void should_expose_the_key_for_read_model_resolution() =>
        _result[CommandContextKeys.ResolvedKey].ShouldEqual(_id.ToString());

    record CommandWithKey([Key] Guid Id);
}
