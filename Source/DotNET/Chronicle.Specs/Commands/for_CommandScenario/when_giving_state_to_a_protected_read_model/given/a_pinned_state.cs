// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_giving_state_to_a_protected_read_model.given;

public class a_pinned_state : Specification
{
    protected readonly EventSourceId _source = EventSourceId.New();
    protected CommandScenario<Decide> _scenario;
    protected State _state;
    protected CommandResult _result;

    void Establish()
    {
        _scenario = new();
        _state = new(Guid.Parse(_source.Value), "pinned");
        _scenario.Given.ForEventSource(_source).ReadModel(_state);
    }

    async Task Destroy() => await _scenario.DisposeAsync();

    [Command, ReadProtection(ReadProtectionMode.On)]
    public record Decide(EventSourceId Id)
    {
        public Decided Handle(State state) => new(state.Status);
    }

    [FromEvent<StateChanged>]
    public record State([property: Key] Guid Id, string Status);

    [EventType]
    public record StateChanged(string Status);

    [EventType]
    public record Decided(string Status);
}
