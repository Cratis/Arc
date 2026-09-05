// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A host may know which files sit alongside the source without holding their text - a compilation carried from
/// another machine, a file removed between being listed and being opened. That is not an error and never stops a
/// document being generated: the screen keeps the one thing that is still known, which is the file realizing it.
/// </summary>
public class a_screen_whose_file_cannot_be_read : Specification
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
        }
        """;

    static readonly DeclaredUserInterfaceFiles _files = new("Library/Authors/Listing/AuthorList.tsx");

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(_files, ("Library/Authors/Listing/Listing.cs", Source));

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Authors/Listing/Listing.cs", Source)).ShouldBeEmpty();
    [Fact] void should_still_recover_the_screen() => _analysis.Slice().Screens.Select(_ => _.Name).ShouldContainOnly(["AuthorList"]);
    [Fact] void should_still_point_at_the_file_realizing_it() => _analysis.Slice().Screens.Single().FilePath.ShouldEqual("Authors/Listing/AuthorList.tsx");
    [Fact] void should_bind_nothing() => _analysis.Slice().Screens.Single().Data.ShouldBeEmpty();
    [Fact] void should_report_only_what_no_screen_states() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.ScreenStructureNotInferred]);
}
