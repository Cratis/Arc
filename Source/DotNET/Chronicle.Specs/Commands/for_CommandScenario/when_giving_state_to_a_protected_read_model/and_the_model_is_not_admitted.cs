// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_giving_state_to_a_protected_read_model;

public class and_the_model_is_not_admitted : Specification
{
    CommandScenario<DecideOnPropertyKey> _scenario;
    CommandResult _result;
    CommandResult _withoutPin;
    readonly EventSourceId _source = EventSourceId.New();

    async Task Establish()
    {
        await using var unpinned = new CommandScenario<DecideOnPropertyKey>();
        _withoutPin = await unpinned.Execute(new(_source), global::Xunit.TestContext.Current.CancellationToken);
        _scenario = new();
        _scenario.Given.ForEventSource(_source).ReadModel(new PropertyKeyedState(_source.Value));
    }

    async Task Because() => _result = await _scenario.Execute(new(_source), global::Xunit.TestContext.Current.CancellationToken);

    [Fact] void should_refuse_the_unsupported_shape() => Assert.Contains("keyed by an event property", _result.ExceptionMessages.Single());
    [Fact] void should_fail_as_it_does_without_a_pin() => _result.ExceptionMessages.ShouldEqual(_withoutPin.ExceptionMessages);
    [Fact] void should_not_commit() => _scenario.AppendedEvents.ShouldBeEmpty();

    async Task Destroy() => await _scenario.DisposeAsync();

    [Command, ReadProtection(ReadProtectionMode.On)]
    public record DecideOnPropertyKey(EventSourceId Id)
    {
        public given.a_pinned_state.Decided Handle(PropertyKeyedState state) => new(state.Id);
    }

    [FromEvent<PropertyStateChanged>(key: nameof(PropertyStateChanged.Id))]
    public record PropertyKeyedState(string Id);

    [EventType]
    public record PropertyStateChanged(string Id);
}
