// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A column written with type arguments is a column all the same, and a dialog written with them runs its command all
/// the same. A column that also has a <c>body</c> or a selection mode does not show its field - the component renders
/// the body or the selection control instead - so it is left out and reported rather than written as showing the field.
/// </summary>
public class from_a_screen_using_generic_and_overridden_columns : Specification
{
    const string AuthorListComponent = """
        import { DataPage } from '@cratis/components/DataPage';
        import { Column } from '@cratis/components/DataTables';
        import { AllAuthors as Authors } from './AllAuthors';

        export const AuthorList = () => (
            <>
                <DataPage<typeof Authors> title="Authors" query={Authors}>
                    <DataPage.Columns>
                        <Column<Author> field="name" header="Name" />
                        <Column<Author> field="id" header="Identifier" body={(author: Author) => author.name} />
                        <Column<Author> field="selected" selectionMode="multiple" />
                    </DataPage.Columns>
                </DataPage>
            </>
        );
        """;

    const string AddAuthorComponent = """
        import { CommandDialog } from '@cratis/components/CommandDialog';
        import { RegisterAuthor } from './RegisterAuthor';

        export const AddAuthor = () => (
            <CommandDialog<RegisterAuthor> command={RegisterAuthor} title="Add author" />
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
    IEnumerable<string> _unread;

    void Because()
    {
        _result = new ScreenplayGenerator(new ApplicationModelAnalyzer(_files), new ScreenplayEmitter())
            .Generate(Analyzed.Compile(_sources), new ScreenplayOptions());
        _compiled = new ScreenplayCompiler().Compile(_result.Source);
        _unread = _result.Diagnostics.Where(_ => _.Code == ScreenplayDiagnosticCodes.ScreenStructureNotInferred).Select(_ => _.Message).ToList();
    }

    bool Says(string text) => _result.Source.Contains(text, StringComparison.Ordinal);

    bool Reports(string text) => _unread.Any(_ => _.Contains(text, StringComparison.Ordinal));

    [Fact] void should_produce_a_document_that_compiles() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_produce_a_document_without_warnings_or_errors() => _compiled.Diagnostics.WithoutTimelineInformation().ShouldBeEmpty();
    [Fact] void should_write_the_title_of_the_generic_data_page() => Says("title \"Authors\"").ShouldBeTrue();
    [Fact] void should_write_a_column_written_with_type_arguments() => Says("column name label \"Name\"").ShouldBeTrue();
    [Fact] void should_write_the_command_of_a_dialog_written_with_type_arguments() => Says("action RegisterAuthor").ShouldBeTrue();
    [Fact] void should_leave_out_a_column_whose_body_overrides_its_field() => Says("column id").ShouldBeFalse();
    [Fact] void should_leave_out_a_column_rendering_a_selection_control() => Says("column selected").ShouldBeFalse();
    [Fact] void should_report_the_columns_with_another_renderer() => _unread.Count(_ => _.Contains("renders its cells with a callback or as a selection control", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_not_say_that_the_dialog_is_unknown() => Reports("CommandDialog").ShouldBeFalse();
}
