// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Arc generates a proxy per query and a component imports it by name, so an import is not a reading of a user
/// interface - it is a name the model already holds and can be held against what the slice really declares. What the
/// binding is then described with comes from the query rather than from the component: the type it returns, and the
/// parameter it is keyed by.
/// </summary>
/// <remarks>
/// The two imports are written differently on purpose. A clause spanning several lines and one renaming what it
/// brings in are both ordinary in a component, and what matters either way is the name the module exports rather
/// than the one the component happens to call it by.
/// </remarks>
public class a_screen_importing_the_queries_of_its_slice : Specification
{
    const string Source = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Authors.Listing;

        [ReadModel]
        public record Author
        {
            public string Id { get; init; } = string.Empty;

            public static IEnumerable<Author> AllAuthors() => [];

            public static Author AuthorById(string id) => new();

            public static IEnumerable<Author> RetiredAuthors() => [];
        }
        """;

    const string Component = """
        import { DataTable } from 'primereact/datatable';
        import { AllAuthors } from './AllAuthors';
        import {
            AuthorById as ById
        } from './index';

        export const AuthorList = () => <DataTable value={AllAuthors.use()} />;
        """;

    static readonly DeclaredUserInterfaceFiles _files = DeclaredUserInterfaceFiles.Holding(
        ("Library/Authors/Listing/AuthorList.tsx", Component));

    ApplicationModelAnalysis _analysis;
    IEnumerable<ScreenDataModel> _data;

    void Establish()
    {
        _analysis = Analyzed.Source(_files, ("Library/Authors/Listing/Listing.cs", Source));
        _data = _analysis.Slice().Screens.Single().Data;
    }

    ScreenDataModel Binding(string query) => _data.Single(_ => _.Query == query);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Authors/Listing/Listing.cs", Source)).ShouldBeEmpty();
    [Fact] void should_bind_every_query_the_component_imports() => _data.Select(_ => _.Query).ShouldEqual(["AllAuthors", "AuthorById"]);
    [Fact] void should_leave_out_a_query_the_component_never_imports() => _data.Select(_ => _.Query).ShouldNotContain("RetiredAuthors");
    [Fact] void should_take_the_type_from_the_query_returning_many() => Binding("AllAuthors").Type.ShouldEqual(new TypeReferenceModel("Author", true, false));
    [Fact] void should_take_the_type_from_the_query_returning_one() => Binding("AuthorById").Type.ShouldEqual(new TypeReferenceModel("Author", false, false));
    [Fact] void should_key_a_binding_by_what_the_query_requires() => Binding("AuthorById").By.ShouldEqual("id");
    [Fact] void should_leave_a_binding_that_requires_nothing_unkeyed() => Binding("AllAuthors").By.ShouldBeNull();
    [Fact] void should_still_point_at_the_file_realizing_the_screen() => _analysis.Slice().Screens.Single().FilePath.ShouldEqual("Authors/Listing/AuthorList.tsx");
    [Fact] void should_report_only_what_no_screen_states() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.ScreenStructureNotInferred]);
}
