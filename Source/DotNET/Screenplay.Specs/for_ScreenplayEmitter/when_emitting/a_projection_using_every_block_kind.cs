// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting;

/// <summary>
/// The projection definition language is the hardest corner of the document to get right - two expression grammars
/// that must not be crossed, several blocks that do not compile when empty, and keys in more than one shape. If
/// every block survives a round trip, the converters are honest.
/// <para>
/// A join names the read model property holding the joined data separately from the property it keys on, and both
/// are read from the model - the event joined from is listed underneath rather than standing in for either.
/// </para>
/// </summary>
public class a_projection_using_every_block_kind : given.an_emitter
{
    ApplicationModel _model;
    ScreenplayEmission _emission;
    RoundTripResult _roundTrip;

    void Establish() => _model = LibraryApplication.Build() with { Slices = [OrderingSlice.Build()] };

    void Because()
    {
        _emission = _emitter.Emit(_model, _options);
        _roundTrip = RoundTrip.For(_emission.Application);
    }

    [Fact] void should_compile_without_errors() => _roundTrip.Errors.ShouldBeEmpty();
    [Fact] void should_compile_without_any_diagnostics() => _roundTrip.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundTrip.Reprinted.ShouldEqual(_roundTrip.Printed);
    [Fact] void should_report_nothing_as_unmappable() => _emission.Diagnostics.ShouldBeEmpty();
    [Fact] void should_name_the_sequence() => Contains("sequence orders").ShouldBeTrue();
    [Fact] void should_turn_automatic_mapping_off_at_the_root() => Contains("no automap").ShouldBeTrue();
    [Fact] void should_emit_the_every_block() => Contains("every").ShouldBeTrue();
    [Fact] void should_exclude_children_from_the_every_block() => Contains("exclude children").ShouldBeTrue();
    [Fact] void should_emit_the_composite_key() => Contains("key OrderKey").ShouldBeTrue();
    [Fact] void should_emit_the_parent_key() => Contains("parent customerId").ShouldBeTrue();
    [Fact] void should_emit_the_event_context_expression() => Contains("lastUpdated = $eventContext.occurred").ShouldBeTrue();
    [Fact] void should_emit_the_caused_by_expression() => Contains("placedBy = $causedBy.name").ShouldBeTrue();
    [Fact] void should_emit_the_event_source_id_expression() => Contains("id = $eventSourceId").ShouldBeTrue();
    [Fact] void should_emit_a_constant_as_a_literal() => Contains(@"status = ""placed""").ShouldBeTrue();
    [Fact] void should_emit_the_increment_mapping() => Contains("increment versions").ShouldBeTrue();
    [Fact] void should_emit_the_decrement_mapping() => Contains("decrement pending").ShouldBeTrue();
    [Fact] void should_emit_the_add_mapping() => Contains("add total by amount").ShouldBeTrue();
    [Fact] void should_emit_the_subtract_mapping() => Contains("subtract refunded by amount").ShouldBeTrue();
    [Fact] void should_emit_the_count_mapping() => Contains("count occurrences").ShouldBeTrue();
    [Fact] void should_emit_the_join_block() => Contains("join customer on customerId").ShouldBeTrue();
    [Fact] void should_list_the_event_joined_from() => Contains("with CustomerRegistered").ShouldBeTrue();
    [Fact] void should_emit_the_mapping_of_the_joined_event() => Contains("customerName = name").ShouldBeTrue();
    [Fact] void should_emit_the_children_block() => Contains("children items identified by lineNumber").ShouldBeTrue();
    [Fact] void should_emit_the_nested_block() => Contains("nested shipping").ShouldBeTrue();
    [Fact] void should_emit_the_remove_with_block() => Contains("remove with OrderCancelled").ShouldBeTrue();
    [Fact] void should_emit_the_remove_via_join_block() => Contains("remove via join on CustomerAccountClosed").ShouldBeTrue();

    bool Contains(string text) => _emission.Source.Contains(text, StringComparison.Ordinal);
}
