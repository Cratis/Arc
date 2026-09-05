// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Arc decides what a caller sends by asking the container whether a parameter's type is a service, which source
/// cannot answer - so what is asked instead is whether a caller could send one at all. An interface cannot be sent
/// and neither can an abstract type: <c>TimeProvider</c>, the clock a query measures a threshold against, is
/// abstract rather than an interface, and reached the document as a parameter typed by a name it never declares and
/// that no caller has ever sent.
/// </summary>
public class a_query_handed_an_abstract_collaborator : Specification
{
    const string Source = """
        using System;
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Authors.Listing;

        public abstract class Clock
        {
            public abstract DateTimeOffset Now { get; }
        }

        [ReadModel]
        public record Author
        {
            public string Id { get; init; } = string.Empty;

            public static IEnumerable<Author> AuthorsSince(string name, TimeProvider time, Clock clock) => [];
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
    [Fact] void should_leave_out_the_clock_the_host_hands_it() => _query.Filters.ShouldBeEmpty();
    [Fact] void should_declare_nothing_for_a_type_no_caller_sends() => _analysis.Model.Concepts.Any(_ => _.Name == "TimeProvider" || _.Name == "Clock").ShouldBeFalse();
}
