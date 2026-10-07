// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_event_constructed_only_by_a_producer_and_specifications : a_generated_document
{
    void Because() => Generate(
        (Analyzed.SlicePath, EventAppendSources.Producer),
        ("Library/Feature/Slice/when_appending/and_it_succeeds.cs", EventAppendSources.With("_first", "_first", "_first")),
        (IntegrationTesting.Path, IntegrationTesting.Source));

    [Fact] void should_declare_the_event_inline() => Result.Source.ShouldContain("produces event AuthorRegistered");
    [Fact] void should_count_only_the_production() => Result.Model.EventProducerCounts.Values.Single().ShouldEqual(1);
    [Fact] void should_keep_the_analyzed_scenario() => Result.Model.Slices.SelectMany(slice => slice.Specifications).ShouldNotBeEmpty();
    [Fact] void should_omit_the_append_only_scenario() => Result.Source.ShouldNotContain("specification");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
