// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_EventSourceValuesProvider.when_providing;

/// <summary>
/// The reflection-based fallback — a command that does not compose its own key — must keep resolving the key from its
/// property. Pinned here so hardening the provided-key path cannot quietly change it.
/// </summary>
public class with_a_command_carrying_a_key_property : Specification
{
    EventSourceValuesProvider _provider;
    CommandContextValues _result;
    Guid _id;

    void Establish()
    {
        _provider = new EventSourceValuesProvider(new RecordingLogger<EventSourceValuesProvider>());
        _id = Guid.NewGuid();
    }

    void Because() => _result = _provider.Provide(new CommandWithKeyProperty(new EventSourceId<Guid>(_id)));

    [Fact] void should_resolve_the_event_source_id_from_the_property() =>
        ((EventSourceId)_result[WellKnownCommandContextKeys.EventSourceId]).Value.ShouldEqual(_id.ToString());

    record CommandWithKeyProperty(EventSourceId<Guid> Id);
}
