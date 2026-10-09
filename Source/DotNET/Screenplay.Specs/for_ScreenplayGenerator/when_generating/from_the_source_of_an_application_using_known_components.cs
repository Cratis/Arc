// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A screen written with Cratis Components states part of its structure through properties whose meaning the
/// components fix: the title of a data page, the query its table is bound to, the field and header of each column,
/// the command a dialog runs. That much is written into the document, and every part the generator cannot read
/// without guessing - a computed header, a column showing a computed value, a menu item running a callback, a
/// details pane, a dialog for a command of another slice - is left out and reported on its own.
/// </summary>
public class from_the_source_of_an_application_using_known_components : Specification
{
    const string AddAuthorComponent = """
        import { CommandDialog } from '@cratis/components/CommandDialog';
        import { RegisterAuthor } from './RegisterAuthor';

        export const AddAuthor = () => (
            <CommandDialog command={RegisterAuthor} title="Add author" okLabel="Add">
                <InputTextField<RegisterAuthor> value={c => c.name} title="Name" />
            </CommandDialog>
        );
        """;

    const string AuthorListComponent = """
        import { DataPage } from '@cratis/components/DataPage';
        import { Column } from '@cratis/components/DataTables';
        import { CommandDialog } from '@cratis/components/CommandDialog';
        import { AllAuthors as Authors } from './AllAuthors';
        import { RegisterAuthor } from '../Registration/RegisterAuthor';
        import { AuthorDetails } from './AuthorDetails';
        import { strings } from '../strings';

        export const AuthorList = () => (
            <>
                <DataPage
                    title="Authors"
                    query={Authors}
                    emptyMessage="No authors"
                    detailsComponent={AuthorDetails}>
                    <DataPage.MenuItems>
                        <DataPage.MenuItem label="Retire" command={() => retire()} disableOnUnselected />
                    </DataPage.MenuItems>
                    <DataPage.Columns>
                        <Column field="name" header="Name" sortable />
                        <Column field="id" header={strings.identifier} />
                        {/* <Column field="retired" header="Retired" /> */}
                        <Column header="Initials" body={(author) => author.name.slice(0, 1)} />
                    </DataPage.Columns>
                </DataPage>
                <CommandDialog command={RegisterAuthor} />
            </>
        );
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Authors/Registration/Registration.cs", LibrarySource.AuthorRegistration),
        ("Library/Authors/Listing/Listing.cs", LibrarySource.AuthorListing)
    ];

    static readonly DeclaredUserInterfaceFiles _files = DeclaredUserInterfaceFiles.Holding(
        ("Library/Authors/Registration/AddAuthor.tsx", AddAuthorComponent),
        ("Library/Authors/Listing/AuthorList.tsx", AuthorListComponent));

    ScreenplayGenerationResult _result;
    CompilationResult<Cratis.Screenplay.Syntax.ApplicationSyntax> _compiled;
    string _reprinted;
    IEnumerable<string> _unread;
    IEnumerable<string> _withheldQueries;

    void Because()
    {
        _result = new ScreenplayGenerator(new ApplicationModelAnalyzer(_files), new ScreenplayEmitter())
            .Generate(Analyzed.Compile(_sources), new ScreenplayOptions());
        _compiled = new ScreenplayCompiler().Compile(_result.Source);
        _reprinted = _compiled.Value is null ? string.Empty : new Cratis.Screenplay.Printing.ScreenplayPrinter().Print(_compiled.Value);
        _unread = _result.Diagnostics.Where(_ => _.Code == ScreenplayDiagnosticCodes.ScreenStructureNotInferred).Select(_ => _.Message).ToList();
        _withheldQueries = _result.Diagnostics
            .Where(_ => _.Code == ScreenplayDiagnosticCodes.UnmappableQuery && _.Severity == ScreenplayDiagnosticSeverity.Information && _.Message.EndsWith("has a body; performer references are authoring-only, so enable ScreenplayOptions.AuthoringOnlyConstructs to include its implementation file", StringComparison.Ordinal))
            .Select(_ => _.Message.Split('\'')[1]).Order().ToList();
    }

    bool Says(string text) => _result.Source.Contains(text, StringComparison.Ordinal);

    bool SaysInSequence(params string[] lines)
    {
        var written = _result.Source.Split('\n').Select(_ => _.Trim()).ToList();
        return Enumerable.Range(0, written.Count - lines.Length + 1).Any(start => written.Skip(start).Take(lines.Length).SequenceEqual(lines));
    }

    bool Reports(string text) => _unread.Any(_ => _.Contains(text, StringComparison.Ordinal));

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_produce_a_document_that_compiles() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_produce_a_document_without_warnings_or_errors() => _compiled.Diagnostics.WithoutTimelineInformation().ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _reprinted.ShouldEqual(_result.Source);
    [Fact] void should_write_the_title_of_the_data_page() => SaysInSequence("data Author[] via query AllAuthors", "title \"Authors\"").ShouldBeTrue();
    [Fact] void should_write_a_table_of_the_read_model_the_query_returns() => SaysInSequence("table Author", "column name label \"Name\"", "column id", string.Empty).ShouldBeTrue();
    [Fact] void should_leave_out_a_column_that_names_no_field() => Says("Initials").ShouldBeFalse();
    [Fact] void should_leave_out_a_column_that_is_commented_out() => Says("column retired").ShouldBeFalse();
    [Fact] void should_write_the_command_a_dialog_runs() => SaysInSequence("action RegisterAuthor").ShouldBeTrue();
    [Fact] void should_write_the_action_only_on_the_screen_of_the_slice_declaring_the_command() => _result.Source.Split("action RegisterAuthor").Length.ShouldEqual(2);
    [Fact] void should_never_write_a_navigation() => Says("navigate to").ShouldBeFalse();
    [Fact] void should_report_the_computed_header() => Reports("heads the column 'id' in the table of 'Author' with 'strings.identifier'").ShouldBeTrue();
    [Fact] void should_report_the_column_that_names_no_field() => Reports("does not name the property it shows as text").ShouldBeTrue();
    [Fact] void should_report_the_menu_item() => Reports("has a menu item 'Retire' that runs a callback").ShouldBeTrue();
    [Fact] void should_report_the_details_component() => Reports("shows a details component beside the <DataPage>").ShouldBeTrue();
    [Fact] void should_report_the_dialog_for_a_command_of_another_slice() => Reports("runs 'RegisterAuthor' from a <CommandDialog>, which is not a command its slice declares").ShouldBeTrue();
    [Fact] void should_report_each_unread_directive_on_its_own() => _unread.Count().ShouldEqual(5);
    [Fact] void should_report_nothing_for_a_screen_read_in_full() => _unread.Any(_ => _.StartsWith("The screen 'AddAuthor'", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_report_nothing_but_what_was_not_inferred_and_the_withheld_query_implementations() => _result.Diagnostics.Select(_ => _.Code).Distinct().ShouldContainOnly([ScreenplayDiagnosticCodes.ScreenStructureNotInferred, ScreenplayDiagnosticCodes.UnmappableQuery]);
    [Fact] void should_report_the_withheld_implementation_of_each_query_with_a_body() => _withheldQueries.ShouldContainOnly(["AllAuthors", "AuthorById"]);
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
}
