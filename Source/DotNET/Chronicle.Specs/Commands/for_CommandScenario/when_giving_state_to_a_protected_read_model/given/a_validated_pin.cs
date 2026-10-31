// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using FluentValidation;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_giving_state_to_a_protected_read_model.given;

public class a_validated_pin : Specification
{
    protected CommandScenario<DecideWithValidator> _scenario;
    protected readonly EventSourceId _source = EventSourceId.New();
    protected a_pinned_state.State _state;
    protected CommandResult _result;

    void Establish()
    {
        _scenario = new();
        _state = new(Guid.Parse(_source.Value), "pinned");
        _scenario.Given.ForEventSource(_source).ReadModel(_state);
    }

    async Task Destroy() => await _scenario.DisposeAsync();

    [Command, ReadProtection(ReadProtectionMode.On)]
    public record DecideWithValidator(EventSourceId Id, a_pinned_state.State Expected)
    {
        public a_pinned_state.Decided Handle(a_pinned_state.State state) =>
            new(ReferenceEquals(state, Expected) ? state.Status : "different instance");
    }

    public class DecideWithValidatorValidator : CommandValidator<DecideWithValidator>
    {
        public DecideWithValidatorValidator(a_pinned_state.State state) =>
            RuleFor(command => command.Expected).Must(expected => ReferenceEquals(expected, state));
    }
}
