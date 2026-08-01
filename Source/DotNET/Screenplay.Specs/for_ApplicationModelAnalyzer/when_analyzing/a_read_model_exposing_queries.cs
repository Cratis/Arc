// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Awaiting, streaming, observing and querying are all how a result arrives, never what it is. A document says what
/// a query returns and whether there is one or many of it, so every wrapper has to be peeled away until that is all
/// that is left.
/// </summary>
public class a_read_model_exposing_queries : Specification
{
    const string Source = """
        using System.Collections.Generic;
        using System.Linq;
        using System.Threading.Tasks;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Authors.Listing;

        [ReadModel]
        public record Author(string Id, string Name)
        {
            public static IEnumerable<Author> AllAuthors() => [];

            public static Task<Author> AuthorById(string id) => Task.FromResult(new Author(id, string.Empty));

            public static IQueryable<Author> AuthorsByName(string name, int take = 10) => Enumerable.Empty<Author>().AsQueryable();
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    QueryModel Query(string name) => _analysis.Slice().Queries.First(_ => _.Name == name);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_recover_every_query() => _analysis.Slice().Queries.Select(_ => _.Name).ShouldContainOnly(["AllAuthors", "AuthorById", "AuthorsByName"]);
    [Fact] void should_return_many_from_a_sequence() => Query("AllAuthors").ReturnType.ShouldEqual(new TypeReferenceModel("Author", true, false));
    [Fact] void should_return_one_from_behind_an_await() => Query("AuthorById").ReturnType.ShouldEqual(new TypeReferenceModel("Author", false, false));
    [Fact] void should_return_many_from_behind_a_queryable() => Query("AuthorsByName").ReturnType.IsCollection.ShouldBeTrue();
    [Fact] void should_take_the_required_parameter_as_the_one_identifying_an_instance() => Query("AuthorById").By!.Name.ShouldEqual("id");
    [Fact] void should_take_no_parameter_when_the_query_needs_none() => Query("AllAuthors").By.ShouldBeNull();
    [Fact] void should_narrow_with_the_remaining_parameters() => Query("AuthorsByName").Filters.Select(_ => _.Name).ShouldContainOnly(["take"]);
    [Fact] void should_infer_a_state_view_slice() => _analysis.Slice().Kind.ShouldEqual(SliceKind.StateView);
    [Fact] void should_say_the_host_pages_the_query_handing_back_a_queryable() => _analysis.Diagnostics.Single().Message.ShouldContain("'AuthorsByName'");
    [Fact] void should_report_that_as_a_serving_concern() => _analysis.Diagnostics.Single().Code.ShouldEqual(ScreenplayDiagnosticCodes.ServingConcernWithoutCounterpart);
    [Fact] void should_say_nothing_about_the_queries_handed_back_whole() => _analysis.Diagnostics.Count(_ => _.Message.Contains("'AllAuthors'", StringComparison.Ordinal) || _.Message.Contains("'AuthorById'", StringComparison.Ordinal)).ShouldEqual(0);
}
