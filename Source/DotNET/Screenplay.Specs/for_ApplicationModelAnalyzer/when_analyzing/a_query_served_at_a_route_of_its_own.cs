// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// An application can serve a read model or a single query somewhere other than where the convention would put it.
/// Where it answers is not something a Screenplay says, so the route is left out - but a route declared and never
/// mentioned reads exactly like an application that declared none.
/// </summary>
public class a_query_served_at_a_route_of_its_own : Specification
{
    const string Source = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Authors.Listing;

        [ReadModel]
        [Path("/catalog/authors")]
        public record Author
        {
            public string Id { get; init; } = string.Empty;

            [Path("/catalog/authors/by-name")]
            public static IEnumerable<Author> AuthorsByName(string name) => [];
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(("Library/Authors/Listing/Listing.cs", Source));

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Authors/Listing/Listing.cs", Source)).ShouldBeEmpty();
    [Fact] void should_still_recover_the_query() => _analysis.Slice().Queries.Single().Name.ShouldEqual("AuthorsByName");
    [Fact] void should_say_where_the_read_model_is_served() => _analysis.Diagnostics.Count(_ => _.Message.Contains("/catalog/authors'", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_say_where_the_query_is_served() => _analysis.Diagnostics.Count(_ => _.Message.Contains("/catalog/authors/by-name'", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_report_both_as_a_serving_concern() => _analysis.Diagnostics.All(_ => _.Code == ScreenplayDiagnosticCodes.ServingConcernWithoutCounterpart).ShouldBeTrue();
}
