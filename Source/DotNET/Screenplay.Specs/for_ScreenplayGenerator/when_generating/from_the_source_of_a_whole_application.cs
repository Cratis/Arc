// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// This is the whole promise, end to end: C# an Arc application could really be written in goes in, and a document
/// that the Screenplay compiler accepts without a single diagnostic comes out - and printing that document again
/// yields byte identical text, which is what proves nothing was lost between the two halves.
/// </summary>
public class from_the_source_of_a_whole_application : Specification
{
    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Authors/Registration/Registration.cs", LibrarySource.AuthorRegistration),
        ("Library/Authors/Listing/Listing.cs", LibrarySource.AuthorListing),
        ("Library/Lending/Reserving/Reserving.cs", LibrarySource.Reserving),
        ("Library/Lending/Notifications/Notifications.cs", LibrarySource.Notifications)
    ];

    ScreenplayGenerationResult _result;
    CompilationResult<Cratis.Screenplay.Syntax.ApplicationSyntax> _compiled;
    string _reprinted;

    void Because()
    {
        _result = new ScreenplayGenerator(
                new ApplicationModelAnalyzer(DeclaredUserInterfaceFiles.None),
                new ScreenplayEmitter())
            .Generate(Analyzed.Compile(_sources), new ScreenplayOptions());
        _compiled = new ScreenplayCompiler().Compile(_result.Source);
        _reprinted = _compiled.Value is null ? string.Empty : new Cratis.Screenplay.Printing.ScreenplayPrinter().Print(_compiled.Value);
    }

    bool Says(string text) => _result.Source.Contains(text, StringComparison.Ordinal);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_produce_a_document_that_compiles() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_produce_a_document_the_compiler_says_nothing_about() => _compiled.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _reprinted.ShouldEqual(_result.Source);
    [Fact] void should_name_the_domain_after_the_compilation() => Says("domain Library").ShouldBeTrue();
    [Fact] void should_declare_the_concepts_the_application_refers_to() => Says("concept AuthorName : String @pii").ShouldBeTrue();
    [Fact] void should_arrange_the_slices_into_features() => Says("feature Authors").ShouldBeTrue();
    [Fact] void should_declare_the_command() => Says("command RegisterAuthor").ShouldBeTrue();
    [Fact] void should_state_what_the_command_produces() => Says("produces AuthorRegistered").ShouldBeTrue();
    [Fact] void should_state_the_condition_a_decision_produces_under() => Says("produces when inStock == true").ShouldBeTrue();
    [Fact] void should_map_the_produced_event_from_the_command_input() => Says("name = name").ShouldBeTrue();
    [Fact] void should_declare_the_validation_rules_with_their_messages() => Says("name not empty message \"An author must have a name\"").ShouldBeTrue();
    [Fact] void should_declare_what_the_command_requires_of_the_caller() => Says("authorize").ShouldBeTrue();
    [Fact] void should_declare_the_constraint() => Says("constraint UniqueAuthorName").ShouldBeTrue();
    [Fact] void should_declare_the_query() => Says("query AllAuthors").ShouldBeTrue();
    [Fact] void should_declare_the_projection() => Says("projection Author").ShouldBeTrue();
    [Fact] void should_declare_the_reactor() => Says("reactor ReservationNotifier").ShouldBeTrue();
    [Fact] void should_read_the_reactor_from_the_file_it_lives_in() => Says("Lending/Notifications/Notifications.cs").ShouldBeTrue();
    [Fact] void should_report_nothing_as_unmappable() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
}
