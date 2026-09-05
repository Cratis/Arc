// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// An import that has been commented out is an import the component does not make, so a binding recovered from one
/// says the screen reads through a query it never calls. A commented import is also the most ordinary thing to find
/// in a component under change, which makes it exactly the case the reader has to be right about.
/// </summary>
public class a_screen_whose_imports_are_commented_out : Specification
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

            public static IEnumerable<Author> RetiredAuthors() => [];

            public static IEnumerable<Author> HonoraryAuthors() => [];
        }
        """;

    const string Component = """
        import { DataTable } from 'primereact/datatable';
        // import { RetiredAuthors } from './RetiredAuthors';
        /*
        import { HonoraryAuthors } from './HonoraryAuthors';
        */
        import { AllAuthors } from './AllAuthors';

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

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Authors/Listing/Listing.cs", Source)).ShouldBeEmpty();
    [Fact] void should_bind_only_the_query_the_component_really_imports() => _data.Select(_ => _.Query).ShouldContainOnly(["AllAuthors"]);
    [Fact] void should_report_only_what_no_screen_states() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.ScreenStructureNotInferred]);
}
