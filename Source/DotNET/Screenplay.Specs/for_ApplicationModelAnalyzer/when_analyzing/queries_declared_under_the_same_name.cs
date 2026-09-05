// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Two read models in one namespace can each declare a query called the same thing. Emitting both under that one
/// name compiles, which is worse than failing - the document reads as if one were a duplicate of the other. Both are
/// therefore qualified by the read model that declares them, so both survive and both are traceable back to source.
/// </summary>
public class queries_declared_under_the_same_name : Specification
{
    const string Source = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Showcase.Listing;

        [ReadModel]
        public record ShowcaseItem(string Id)
        {
            public static IEnumerable<ShowcaseItem> ObserveAll() => [];

            public static IEnumerable<ShowcaseItem> OnlyHere() => [];
        }

        [ReadModel]
        public record AuditItem(string Id)
        {
            public static IEnumerable<AuditItem> ObserveAll() => [];
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_keep_both_queries() => _analysis.Slice().Queries.Count().ShouldEqual(3);
    [Fact] void should_qualify_every_colliding_name_by_its_read_model() => _analysis.Slice().Queries.Select(_ => _.Name).ShouldContainOnly(["AuditItemObserveAll", "ShowcaseItemObserveAll", "OnlyHere"]);
    [Fact] void should_leave_a_name_that_collides_with_nothing_alone() => _analysis.Slice().Queries.Any(_ => _.Name == "OnlyHere").ShouldBeTrue();
    [Fact] void should_report_each_query_it_renamed() => _analysis.Diagnostics.Count(_ => _.Code == ScreenplayDiagnosticCodes.AmbiguousQueryName).ShouldEqual(2);
    [Fact] void should_report_it_as_information_since_nothing_was_lost() => _analysis.Diagnostics.Where(_ => _.Code == ScreenplayDiagnosticCodes.AmbiguousQueryName).All(_ => _.Severity == ScreenplayDiagnosticSeverity.Information).ShouldBeTrue();
}
