// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Concepts;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_giving_state_to_a_protected_read_model;

public class and_the_lookup_uses_look_up_by : Specification
{
    readonly ProductId _product = new(Guid.NewGuid());
    readonly EventSourceId _source = EventSourceId.New();
    CommandScenario<DecideOnProduct> _scenario;
    CommandResult _result;

    void Establish()
    {
        _scenario = new();
        _scenario.Given.ForEventSource(_product.Value.ToString()).ReadModel(new given.a_pinned_state.State(_product.Value, "product pin"));
    }

    async Task Because() => _result = await _scenario.Execute(new(_source, _product), global::Xunit.TestContext.Current.CancellationToken);

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] async Task should_find_the_pin_by_the_lookup_key() =>
        await _scenario.ShouldHaveAppendedEvent<DecideOnProduct, given.a_pinned_state.Decided>(_source, fact => fact.Status == "product pin");

    async Task Destroy() => await _scenario.DisposeAsync();

    public record ProductId(Guid Value) : ConceptAs<Guid>(Value);

    [Command, ReadProtection(ReadProtectionMode.On)]
    public record DecideOnProduct(EventSourceId Id, ProductId Product)
    {
        public given.a_pinned_state.Decided Handle([LookUpBy(nameof(Product))] given.a_pinned_state.State state) => new(state.Status);
    }
}
