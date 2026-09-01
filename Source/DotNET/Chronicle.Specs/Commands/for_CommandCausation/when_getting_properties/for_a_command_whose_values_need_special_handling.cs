// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands.for_CommandCausation.when_getting_properties.given;

namespace Cratis.Arc.Chronicle.Commands.for_CommandCausation.when_getting_properties;

/// <summary>
/// Not every value renders as a word. A property named like the causation's own metadata would make an event
/// misreport what produced it, an unset one has nothing to say, and an unbounded one is written again on every event
/// the command appends - which is a payload, not an audit note.
/// </summary>
public class for_a_command_whose_values_need_special_handling : Specification
{
    record ArchiveExpenseReports(
        string CommandType,
        string EventSequenceId,
        string? Note,
        Approver ApprovedBy,
        IEnumerable<string> ReportIds,
        string Justification);

    const int OversizedLength = 4000;

    IDictionary<string, string> _properties;

    void Because() => _properties = CommandCausation.PropertiesFor(
        typeof(ArchiveExpenseReports),
        new ArchiveExpenseReports(
            "pretending to be the metadata",
            "pretending to be the sequence",
            null,
            new("Jane", "Finance"),
            ["first", "second"],
            new string('x', OversizedLength)));

    [Fact] void should_keep_the_causation_metadata_the_command_tried_to_occupy() =>
        _properties[CommandCausation.CommandTypeProperty].ShouldEqual(nameof(ArchiveExpenseReports));

    [Fact] void should_not_let_a_command_write_a_reserved_property() =>
        _properties.Values.Contains("pretending to be the metadata").ShouldBeFalse();

    [Fact] void should_not_let_a_command_write_the_reserved_sequence_property() =>
        _properties.ContainsKey(CommandCausation.EventSequenceIdProperty).ShouldBeFalse();

    [Fact] void should_leave_out_a_value_that_was_never_set() =>
        _properties.ContainsKey("note").ShouldBeFalse();

    [Fact] void should_render_a_nested_value_as_json() =>
        _properties["approvedBy"].ShouldEqual("""{"Name":"Jane","Department":"Finance"}""");

    [Fact] void should_render_a_collection_as_json() =>
        _properties["reportIds"].ShouldEqual("""["first","second"]""");

    [Fact] void should_cut_an_oversized_value_short() =>
        _properties["justification"].Length.ShouldEqual(CommandCausationValues.MaximumValueLength + CommandCausationValues.TruncationMarker.Length);

    [Fact] void should_mark_a_value_it_cut_short() =>
        _properties["justification"].EndsWith(CommandCausationValues.TruncationMarker).ShouldBeTrue();
}
