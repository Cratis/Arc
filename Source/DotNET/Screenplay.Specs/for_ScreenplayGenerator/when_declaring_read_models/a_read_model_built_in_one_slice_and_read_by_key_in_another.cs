// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

/// <summary>
/// A read model is declared once in a document, and the slice holding its keyed query is the one declaring it - even
/// when another slice, earlier in namespace order, holds what builds it. The executable model identifies an instance
/// through the keyed query declared beside the read model, so declaring it next to the projection instead would leave
/// an identity the application really has unstated.
/// </summary>
public class a_read_model_built_in_one_slice_and_read_by_key_in_another : a_read_model_document
{
    const string Assembling = """
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections;
        using Library.Inventory.Lookup;

        namespace Library.Inventory.Assembling;

        [EventType]
        public record StockCounted(int Count);

        public class StockProjection : IProjectionFor<Stock>
        {
            public void Define(IProjectionBuilderFor<Stock> builder) => builder.From<StockCounted>();
        }
        """;

    const string Lookup = """
        using System;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Inventory.Lookup;

        [ReadModel]
        public record Stock(Guid Id, int Count)
        {
            public static Stock? ById(Guid id) => null;
        }
        """;

    void Because() => Generate(("Library/Inventory/Assembling/Assembling.cs", Assembling), ("Library/Inventory/Lookup/Lookup.cs", Lookup));

    IEnumerable<string> DeclaredIn(string @namespace) =>
        Result.Model.Slices.Single(_ => _.Namespace == @namespace).ReadModels.Select(_ => _.Name);

    [Fact] void should_declare_it_in_the_slice_holding_its_keyed_query() => DeclaredIn("Library.Inventory.Lookup").ShouldContainOnly(["Stock"]);
    [Fact] void should_not_declare_it_beside_the_projection() => DeclaredIn("Library.Inventory.Assembling").ShouldBeEmpty();
    [Fact] void should_declare_it_once() => Result.Source.Split('\n').Count(_ => _.Trim() == "readmodel Stock").ShouldEqual(1);
    [Fact] void should_resolve_every_reference() => UnresolvedReferences.ShouldBeEmpty();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
