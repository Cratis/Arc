// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting;

/// <summary>
/// The whole point of the rule is what it does to a real document, so this is the case end to end: a command with a
/// property called <c>Description</c>, an event with one called <c>Tag</c>, and a produces block filling that same
/// property in. Written out as they are, three lines are read as directives instead of names and the document stops
/// compiling. Left out and reported, everything around them survives intact.
/// </summary>
public class a_slice_naming_its_members_after_directives : given.an_emitter
{
    ApplicationModel _model;
    ScreenplayEmission _emission;
    RoundTripResult _roundTrip;

    void Establish() =>
        _model = LibraryApplication.Build() with { Slices = [Requesting()] };

    void Because()
    {
        _emission = _emitter.Emit(_model, _options);
        _roundTrip = RoundTrip.For(_emission.Application);
    }

    bool Says(string text) => _emission.Source.Contains(text, StringComparison.Ordinal);

    [Fact] void should_compile_without_errors() => _roundTrip.Errors.ShouldBeEmpty();
    [Fact] void should_compile_without_any_diagnostics() => _roundTrip.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundTrip.Reprinted.ShouldEqual(_roundTrip.Printed);
    [Fact] void should_not_write_the_command_property_the_body_reserves() => Says("description RequestDescription").ShouldBeFalse();
    [Fact] void should_not_write_the_event_property_the_body_reserves() => Says("tag BookTag").ShouldBeFalse();
    [Fact] void should_not_write_the_mapping_the_produces_block_reserves() => Says("tag = tag").ShouldBeFalse();
    [Fact] void should_keep_the_command_property_it_can_write() => Says("title BookTitle").ShouldBeTrue();
    [Fact] void should_keep_the_mapping_it_can_write() => Says("title = title").ShouldBeTrue();
    [Fact] void should_report_every_line_it_left_out() => _emission.Diagnostics.Select(_ => _.Code).ShouldContainOnly(
    [
        ScreenplayDiagnosticCodes.NameReservedByGrammar,
        ScreenplayDiagnosticCodes.NameReservedByGrammar,
        ScreenplayDiagnosticCodes.NameReservedByGrammar
    ]);

    static SliceModel Requesting() =>
        new(
            "Library.Lending.Requesting",
            "Requesting",
            SliceKind.StateChange,
            null,
            [
                new CommandModel(
                    "RequestBook",
                    null,
                    [
                        Declare.Property("Title", "BookTitle"),
                        Declare.Property("Description", "RequestDescription")
                    ],
                    null,
                    [],
                    [
                        new ProducesModel(
                            "BookRequested",
                            null,
                            [
                                Declare.From("Title", "Title"),
                                Declare.From("Tag", "Tag")
                            ])
                    ],
                    null,
                    "Lending/Requesting/Requesting.cs")
            ],
            [
                new EventModel(
                    "BookRequested",
                    [
                        Declare.Property("Title", "BookTitle"),
                        Declare.Property("Tag", "BookTag")
                    ],
                    [])
            ],
            [],
            [],
            [],
            []);
}
