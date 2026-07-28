// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Arc serves a static method on a read model as a query when it returns that read model, and its accessibility
/// never enters into it. Both halves matter: a helper returning something else is not a route, and stating it as one
/// puts an endpoint in the document that no application serves, while a query that happens not to be public is a
/// route the application really does serve.
/// </summary>
public class a_read_model_carrying_more_than_its_queries : Specification
{
    const string Source = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Authors.Listing;

        [ReadModel]
        public record Author(string Id, string Name)
        {
            public static IEnumerable<Author> AllAuthors() => [];

            internal static Author AuthorById(string id) => new(id, string.Empty);

            public static int CountOf(IEnumerable<Author> authors) => 0;

            public static string DisplayNameOf(Author author) => author.Name;
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_recover_only_what_returns_the_read_model() => _analysis.Slice().Queries.Select(_ => _.Name).ShouldContainOnly(["AllAuthors", "AuthorById"]);
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
