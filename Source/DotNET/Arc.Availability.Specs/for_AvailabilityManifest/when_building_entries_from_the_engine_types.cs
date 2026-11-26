// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;

namespace Cratis.Arc.Availability.for_AvailabilityManifest;

public class when_building_entries_from_the_engine_types : Specification
{
    IAvailabilityManifest _manifest;
    AvailabilityDefinition _definition;
    ContextValueEntry _value;

    void Because()
    {
        _definition = AvailabilityDefinition.FromLines("Staff", "Internal people", AvailabilityExpression.For("Admin", "Operator"));
        var catalog = new AvailabilityCatalog().Add(_definition);
        var provider = new FixedProvider();
        _value = ContextValueEntry.From(new ContextValueDeclaration("vessel", provider, ContextScope.Of("Onboarding"), false), "Vessel");

        _manifest = new InMemoryAvailabilityManifest(new AvailabilityManifest(
            "model.play",
            [new AudienceDeclaration("Admin", "Runs the place", "Onboarding")],
            [AvailabilityBlockEntry.From(_definition, catalog, "Onboarding")],
            [AvailabilityGateEntry.From("Onboarding/AddVessel", AvailabilityExpression.With("Staff"), catalog)],
            [_value],
            [new PersonaEntry("Sam", ["Admin"])]));
    }

    [Fact] void should_hold_the_origin() => _manifest.Current.Origin.ShouldEqual("model.play");
    [Fact] void should_keep_the_block_description() => _manifest.Current.Blocks.Single().Description.ShouldEqual("Internal people");
    [Fact] void should_expand_the_block() => AlternativeText.Of(_manifest.Current.Blocks.Single().Alternatives).ShouldEqual("Admin|Operator");
    [Fact] void should_expand_the_gate_through_with() => AlternativeText.Of(_manifest.Current.Gates.Single().Alternatives).ShouldEqual("Admin|Operator");
    [Fact] void should_carry_context_value_scope_and_requirement() => (_value.Scope.Path.Single() == "Onboarding" && !_value.Required).ShouldBeTrue();
    [Fact] void should_default_the_origin_of_a_value_to_the_provider() => _value.Origin.ShouldEqual(nameof(FixedProvider));
    [Fact] void should_hold_the_persona() => _manifest.Current.Personas.Single().Audiences.Single().Value.ShouldEqual("Admin");

    class FixedProvider : IContextValueProvider
    {
        public IReadOnlyCollection<string> Dependencies => [];

        public ValueTask<ContextValueOutcome> Provide(IContextValueResolver context, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
