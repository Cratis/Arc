// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

public class from_a_query_keyed_by_an_event_source_id_concept : a_protected_document
{
    const string View = """
        [ReadModel]
        public record Current(Guid Id)
        {
            public static Current? ById(IReadModels readModels, RoomId id) => null;
        }
        """;

    void Because() => Generate(Library(string.Empty, View));

    [Fact] void should_keep_the_query_key_as_the_concept() => Result.Source.Contains("by id RoomId").ShouldBeTrue();
    [Fact] void should_declare_the_concept() => Result.Source.Contains("concept RoomId : Uuid").ShouldBeTrue();
    [Fact] void should_not_replace_the_query_key_with_the_property_type() => Result.Source.Contains("by id Uuid").ShouldBeFalse();
    [Fact] void should_round_trip() => RoundTrip.IsStable.ShouldBeTrue();
}
