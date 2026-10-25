// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

public class from_a_query_with_an_event_source_id_key_property : a_protected_document
{
    const string View = """
        [ReadModel]
        public record Current(EventSourceId Id)
        {
            public static Current? ById(IReadModels readModels, EventSourceId id) => null;
        }
        """;

    void Because() => Generate(Library(string.Empty, View));

    [Fact] void should_keep_the_query_key_as_event_source_id() => Result.Source.Contains("by id EventSourceId").ShouldBeTrue();
    [Fact] void should_declare_event_source_id_as_a_concept() => Result.Source.Contains("concept EventSourceId : String").ShouldBeTrue();
    [Fact] void should_not_replace_the_query_key_with_its_primitive() => Result.Source.Contains("by id String").ShouldBeFalse();
    [Fact] void should_round_trip() => RoundTrip.IsStable.ShouldBeTrue();
}
