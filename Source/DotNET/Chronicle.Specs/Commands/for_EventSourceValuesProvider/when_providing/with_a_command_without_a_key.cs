// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_EventSourceValuesProvider.when_providing;

/// <summary>
/// A command with no key at all opens a fresh stream, so it gets a brand new id — distinct from the unspecified id that
/// signals "there was a key and it could not be composed". Pinned so the two outcomes never collapse into one.
/// </summary>
public class with_a_command_without_a_key : Specification
{
    EventSourceValuesProvider _provider;
    CommandContextValues _result;

    void Establish() => _provider = new EventSourceValuesProvider();

    void Because() => _result = _provider.Provide(new CommandWithoutKey("something"));

    [Fact] void should_provide_a_new_event_source_id() =>
        ((EventSourceId)_result[WellKnownCommandContextKeys.EventSourceId]).ShouldNotEqual(EventSourceId.Unspecified);

    record CommandWithoutKey(string Name);
}
