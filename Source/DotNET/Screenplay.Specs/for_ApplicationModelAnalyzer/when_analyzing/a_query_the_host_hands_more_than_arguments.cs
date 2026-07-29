// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// What a caller sends is what the document describes a query by. A cancellation token, the page asked for and the
/// order asked for are all filled in by the host from the request rather than sent as arguments, and none of them is
/// an interface - so stating them as caller input puts parameters in the document that no caller sends, typed by
/// names the document never declares.
/// </summary>
public class a_query_the_host_hands_more_than_arguments : Specification
{
    const string Source = """
        using System.Collections.Generic;
        using System.Threading;
        using Cratis.Arc.Queries;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Authors.Listing;

        [ReadModel]
        public record Author
        {
            public string Id { get; init; } = string.Empty;

            public static IEnumerable<Author> AuthorsByName(
                string name,
                CancellationToken cancellationToken,
                Paging paging,
                Sorting sorting,
                QueryContext context) => [];
        }
        """;

    ApplicationModelAnalysis _analysis;
    QueryModel _query;

    void Establish()
    {
        _analysis = Analyzed.Source(("Library/Authors/Listing/Listing.cs", Source));
        _query = _analysis.Slice().Queries.Single();
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Authors/Listing/Listing.cs", Source)).ShouldBeEmpty();
    [Fact] void should_key_the_query_by_what_the_caller_sends() => _query.By!.Name.ShouldEqual("name");
    [Fact] void should_leave_out_everything_the_host_fills_in() => _query.Filters.ShouldBeEmpty();
    [Fact] void should_say_the_query_is_paged() => _analysis.Diagnostics.Count(_ => _.Code == ScreenplayDiagnosticCodes.ServingConcernWithoutCounterpart && _.Message.Contains("paging", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_say_the_query_is_sorted() => _analysis.Diagnostics.Count(_ => _.Code == ScreenplayDiagnosticCodes.ServingConcernWithoutCounterpart && _.Message.Contains("sorting", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_say_nothing_about_the_plumbing_the_host_fills_in_of_its_own() => _analysis.Diagnostics.Count.ShouldEqual(2);
}
