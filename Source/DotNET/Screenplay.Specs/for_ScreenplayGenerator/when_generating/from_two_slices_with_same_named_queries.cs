// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_two_slices_with_same_named_queries : a_generated_document
{
    const string Authors = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Authors.Listing;

        [ReadModel]
        public record Author(string AuthorId, string Name)
        {
            public static IEnumerable<Author> All() => [];
        }
        """;

    const string Books = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Books.Listing;

        [ReadModel]
        public record Book(string BookId, string Title)
        {
            public static IEnumerable<Book> All() => [];
        }
        """;

    IEnumerable<Diagnostic> BindingErrors => Bound.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

    void Because() => Generate(("Library/Authors/Listing/Author.cs", Authors), ("Library/Books/Listing/Book.cs", Books));

    [Fact] void should_preserve_both_query_names() => Result.Source.Split("query All", StringSplitOptions.None).Length.ShouldEqual(3);
    [Fact] void should_exercise_the_binder_limit() => BindingErrors.ShouldNotBeEmpty();
    [Fact] void should_report_only_the_documented_ambiguity() => BindingErrors.Select(diagnostic => diagnostic.Message).Distinct().ShouldContainOnly("Query reference 'All' is ambiguous across slices in the current ESM v1 binder.");
    [Fact] void should_tolerate_only_the_known_errors_in_default_mode() => BindingErrors.All(diagnostic => ExpectedBindingDiagnostics.IsExpected(diagnostic, false)).ShouldBeTrue();
    [Fact] void should_tolerate_only_the_known_errors_in_authoring_mode() => BindingErrors.All(diagnostic => ExpectedBindingDiagnostics.IsExpected(diagnostic, true)).ShouldBeTrue();
    [Fact] void should_not_report_sp0056() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).ShouldBeFalse();
    [Fact] void should_generate_without_warnings() => Result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Warning).ShouldBeEmpty();
    [Fact] void should_round_trip() => RoundTrip.IsStable.ShouldBeTrue();
}
