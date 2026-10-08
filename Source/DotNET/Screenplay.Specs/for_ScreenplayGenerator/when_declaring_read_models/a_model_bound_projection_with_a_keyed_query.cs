// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

/// <summary>
/// The shape the first executable vertical admits end to end: a read model a model-bound projection builds, read by a
/// query answering with at most one instance for the key it is given. Declaring the read model is what lets the
/// projection and the query bind at all, and the keyed query beside it is what identifies an instance.
/// </summary>
public class a_model_bound_projection_with_a_keyed_query : a_read_model_document
{
    const string Source = """
        using System;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections.ModelBound;

        namespace Library.Inventory.Listing;

        [EventType]
        public record BookAddedToInventory(string Title, int Count);

        /// <summary>
        /// A book on the shelves and how many of it there are.
        /// </summary>
        [ReadModel]
        [FromEvent<BookAddedToInventory>]
        public record Book(Guid Id, string Title, int Count)
        {
            public static Book? ById(Guid id) => null;
        }
        """;

    void Because() => GenerateSlice(Source);

    [Fact] void should_declare_the_read_model() => Says("readmodel Book").ShouldBeTrue();
    [Fact] void should_describe_it_with_its_summary() => Says("description \"A book on the shelves and how many of it there are.\"").ShouldBeTrue();
    [Fact] void should_name_the_file_declaring_it() => Says("file Feature/Slice/Slice.cs").ShouldBeTrue();
    [Fact] void should_declare_what_it_holds() => (Says("id Uuid") && Says("title String") && Says("count Int")).ShouldBeTrue();
    [Fact] void should_not_claim_an_identifier_on_the_declaration() => Result.Source.ShouldNotContain("Uuid identifier");
    [Fact] void should_key_the_query_by_id() => Says("by id Uuid").ShouldBeTrue();
    [Fact] void should_declare_it_once() => Result.Source.Split('\n').Count(_ => _.Trim() == "readmodel Book").ShouldEqual(1);
    [Fact] void should_resolve_every_reference() => UnresolvedReferences.ShouldBeEmpty();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
