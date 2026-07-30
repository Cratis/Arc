// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_EventSourceValuesProvider.when_building_command_context_values;

/// <summary>
/// Arc reads a command's key itself only when nothing wrote one, so what keeps an application with Chronicle resolving
/// keys exactly as it always has is that Chronicle writes one for every command — including the empty key that says the
/// command carried nothing usable. A command shape that left the key unwritten would quietly hand key resolution to
/// Arc's own rules, which know nothing of event source ids.
/// </summary>
public class with_any_command_at_all : Specification
{
    CommandContextValuesBuilder _builder;
    List<CommandContextValues> _results;

    void Establish() => _builder = new CommandContextValuesBuilder(
        new KnownInstancesOf<ICommandContextValuesProvider>([new EventSourceValuesProvider(new RecordingLogger<EventSourceValuesProvider>())]));

    void Because() => _results =
    [
        _builder.Build(new CarriesAKey(Guid.NewGuid())),
        _builder.Build(new CarriesNoKey("Alice")),
        _builder.Build(new ProvidesAKeyItCannotCompose(null!))
    ];

    [Fact] void should_write_a_resolved_key_for_every_command() =>
        _results.TrueForAll(values => values.ContainsKey(CommandContextKeys.ResolvedKey)).ShouldBeTrue();

    [Fact] void should_write_the_event_source_id_as_the_resolved_key() =>
        _results[0][CommandContextKeys.ResolvedKey].ShouldEqual(((EventSourceId)_results[0][WellKnownCommandContextKeys.EventSourceId]).Value);

    [Fact] void should_write_an_empty_resolved_key_when_the_command_carried_nothing_usable() =>
        _results[2][CommandContextKeys.ResolvedKey].ShouldEqual(string.Empty);

    record CarriesAKey([property: Cratis.Chronicle.Keys.Key] Guid Id);

    record CarriesNoKey(string Name);

    record ProvidesAKeyItCannotCompose(EventSourceId<Guid> Id) : ICanProvideEventSourceId
    {
        public EventSourceId GetEventSourceId() => Id;
    }
}
