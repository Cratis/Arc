// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Chronicle tags read models as readily as it tags events, and only the event has somewhere in the document to carry
/// them - a read model is named there only as what a query answers with. Passing the tags over without a word would
/// leave a reader who sees tags throughout the events concluding that the read models carry none.
/// </summary>
public class a_read_model_carrying_tags : Specification
{
    const string Source = """
        using System.Collections.Generic;
        using Cratis.Chronicle;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Authors.Listing;

        [ReadModel]
        [Tag("audit")]
        [Tags("authors")]
        public record Author
        {
            public string Id { get; init; } = string.Empty;

            public static IEnumerable<Author> AllAuthors() => [];
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(("Library/Authors/Listing/Listing.cs", Source));

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Authors/Listing/Listing.cs", Source)).ShouldBeEmpty();
    [Fact] void should_still_recover_the_query() => _analysis.Slice().Queries.Single().Name.ShouldEqual("AllAuthors");
    [Fact] void should_say_the_tags_have_nowhere_to_go() => _analysis.Diagnostics.Count(_ => _.Code == ScreenplayDiagnosticCodes.ReadModelFeatureWithoutCounterpart).ShouldEqual(1);
    [Fact] void should_name_every_tag_it_could_not_carry() => _analysis.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.ReadModelFeatureWithoutCounterpart).Message.ShouldContain("'audit', 'authors'");
    [Fact] void should_report_it_once_for_the_read_model_rather_than_once_per_tag() => _analysis.Diagnostics.Count.ShouldEqual(1);
}
