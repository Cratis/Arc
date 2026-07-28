// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// The whole promise with the screens in it: source an Arc application could really be written in, with the
/// components a vertical slice puts next to it, produces a document the Screenplay compiler accepts without a single
/// diagnostic - and printing that document again yields byte identical text, which is what proves the screens and
/// the queries they bind survived the trip.
/// </summary>
public class from_the_source_of_an_application_with_screens : Specification
{
    const string AddAuthorComponent = """
        import { CommandDialog } from '@cratis/components/CommandDialog';
        import { RegisterAuthor } from './RegisterAuthor';

        export const AddAuthor = () => <CommandDialog command={RegisterAuthor} />;
        """;

    const string AuthorListComponent = """
        import { DataTable } from 'primereact/datatable';
        import { AllAuthors } from './AllAuthors';
        import { AuthorById } from './AuthorById';

        export const AuthorList = () => <DataTable value={AllAuthors.use()} />;
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Authors/Registration/Registration.cs", LibrarySource.AuthorRegistration),
        ("Library/Authors/Listing/Listing.cs", LibrarySource.AuthorListing)
    ];

    static readonly DeclaredUserInterfaceFiles _files = DeclaredUserInterfaceFiles.Holding(
        ("Library/Authors/Registration/AddAuthor.tsx", AddAuthorComponent),
        ("Library/Authors/Listing/AuthorList.tsx", AuthorListComponent),
        ("Library/Authors/Listing/AuthorList.stories.tsx", AuthorListComponent));

    ScreenplayGenerationResult _result;
    CompilationResult<Cratis.Screenplay.Syntax.ApplicationSyntax> _compiled;
    string _reprinted;

    void Because()
    {
        _result = new ScreenplayGenerator(new ApplicationModelAnalyzer(_files), new ScreenplayEmitter())
            .Generate(Analyzed.Compile(_sources), new ScreenplayOptions());
        _compiled = new ScreenplayCompiler().Compile(_result.Source);
        _reprinted = _compiled.Value is null ? string.Empty : new Cratis.Screenplay.Printing.ScreenplayPrinter().Print(_compiled.Value);
    }

    bool Says(string text) => _result.Source.Contains(text, StringComparison.Ordinal);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_produce_a_document_that_compiles() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_produce_a_document_the_compiler_says_nothing_about() => _compiled.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _reprinted.ShouldEqual(_result.Source);
    [Fact] void should_declare_the_screen_of_the_state_changing_slice() => Says("screen AddAuthor").ShouldBeTrue();
    [Fact] void should_declare_the_screen_of_the_state_viewing_slice() => Says("screen AuthorList").ShouldBeTrue();
    [Fact] void should_refer_to_the_file_realizing_the_screen() => Says("file Authors/Registration/AddAuthor.tsx").ShouldBeTrue();
    [Fact] void should_bind_the_query_returning_many() => Says("data Author[] via query AllAuthors").ShouldBeTrue();
    [Fact] void should_bind_the_query_keyed_by_a_parameter() => Says("data Author via query AuthorById by id").ShouldBeTrue();
    [Fact] void should_leave_out_the_companion_of_a_component() => Says("AuthorList.stories").ShouldBeFalse();
    [Fact] void should_bind_nothing_on_a_screen_that_imports_only_a_command() => Says("via query RegisterAuthor").ShouldBeFalse();
    [Fact] void should_report_only_what_no_screen_states() => _result.Diagnostics.Select(_ => _.Code).Distinct().ShouldContainOnly([ScreenplayDiagnosticCodes.ScreenStructureNotInferred]);
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
}
