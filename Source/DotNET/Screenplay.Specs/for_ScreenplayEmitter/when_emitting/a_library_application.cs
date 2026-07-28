// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting;

/// <summary>
/// The round trip is the correctness gate for everything the generator produces. Output that does not compile means
/// the generator is wrong, and output that does not print identically the second time means information was lost.
/// </summary>
public class a_library_application : given.a_library_model
{
    ScreenplayEmission _emission;
    ModuleSyntax _module;
    RoundTripResult _roundTrip;

    void Because()
    {
        _emission = _emitter.Emit(_model, _options);
        _module = _emission.Application.Modules.Single();
        _roundTrip = RoundTrip.For(_emission.Application);
    }

    [Fact] void should_compile_without_errors() => _roundTrip.Errors.ShouldBeEmpty();
    [Fact] void should_compile_without_any_diagnostics() => _roundTrip.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundTrip.Reprinted.ShouldEqual(_roundTrip.Printed);
    [Fact] void should_report_nothing_as_unmappable() => _emission.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_what_it_returns() => _emission.Source.ShouldEqual(_roundTrip.Printed);
    [Fact] void should_name_the_domain_after_the_application() => _emission.Application.Domain!.Name.ShouldEqual("Library");
    [Fact] void should_name_the_module_after_the_application() => _module.Name.ShouldEqual("Library");
    [Fact] void should_have_a_feature_per_top_level_namespace() => _module.Features.Select(_ => _.Name).ShouldContainOnly(["Authors", "Inventory", "Lending"]);
    [Fact] void should_have_a_slice_per_slice_namespace() => Slices().Select(_ => _.Name).ShouldContainOnly(["Registration", "Listing", "Adding", "Listing", "Reserving", "Notifications", "Restocking"]);
    [Fact] void should_carry_the_kind_of_a_translating_slice() => Slice("Restocking").Type.ShouldEqual(SliceType.Translate);
    [Fact] void should_carry_the_kind_of_an_automating_slice() => Slice("Notifications").Type.ShouldEqual(SliceType.Automation);
    [Fact] void should_carry_the_kind_of_a_state_changing_slice() => Slice("Registration").Type.ShouldEqual(SliceType.StateChange);
    [Fact] void should_carry_the_kind_of_a_state_viewing_slice() => Slice("Adding").Type.ShouldEqual(SliceType.StateChange);
    [Fact] void should_declare_every_concept_of_the_application() => _emission.Application.Concepts.Select(_ => _.Name).ShouldContainOnly(["AuthorId", "AuthorName", "BookTitle", "CopyCount", "ISBN", "MemberId", "MembershipTier"]);
    [Fact] void should_order_the_concepts_by_name() => _emission.Application.Concepts.Select(_ => _.Name).ShouldEqual(_emission.Application.Concepts.Select(_ => _.Name).Order(StringComparer.Ordinal));

    SliceSyntax Slice(string name) => Slices().First(_ => _.Name == name);

    IEnumerable<SliceSyntax> Slices() => _module.Features.SelectMany(_ => _.Slices);
}
