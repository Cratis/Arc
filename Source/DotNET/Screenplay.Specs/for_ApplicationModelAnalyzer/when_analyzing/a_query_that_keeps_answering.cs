// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Handing back a subject is the whole of how a query says it is a live read - there is no attribute saying so, and
/// the caller subscribes rather than asks. Every other wrapper really is only how a result arrives, so peeling them
/// all away alike took a fact about the application with them: a document said the same thing of a query answering
/// once and a query that keeps answering, and said nothing about the difference.
/// </summary>
public class a_query_that_keeps_answering : Specification
{
    const string Reactive = """
        namespace System.Reactive.Subjects
        {
            public interface ISubject<T>
            {
            }
        }
        """;

    const string Source = """
        using System.Collections.Generic;
        using System.Reactive.Subjects;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Authors.Listing;

        [ReadModel]
        public record Author(string Id, string Name)
        {
            public static ISubject<IEnumerable<Author>> AllAuthors() => null!;

            public static IEnumerable<Author> AuthorsOnce() => [];
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Feature/Slice/Slice.cs", Source),
        ("Library/Reactive.cs", Reactive)
    ];

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(_sources);

    QueryModel Query(string name) => _analysis.Slice().Queries.First(_ => _.Name == name);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_state_that_it_keeps_answering() => Query("AllAuthors").IsObservable.ShouldBeTrue();
    [Fact] void should_still_say_what_it_answers_with() => Query("AllAuthors").ReturnType.ShouldEqual(new TypeReferenceModel("Author", true, false));
    [Fact] void should_not_say_it_of_a_query_answering_once() => Query("AuthorsOnce").IsObservable.ShouldBeFalse();
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
