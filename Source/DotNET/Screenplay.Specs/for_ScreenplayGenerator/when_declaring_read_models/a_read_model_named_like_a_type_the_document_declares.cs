// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

/// <summary>
/// A read model whose name a type declaration already uses - here a record an event carries. Declaring the read
/// model as well would name two declarations the same, so the read model is not declared and the type is kept.
/// </summary>
public class a_read_model_named_like_a_type_the_document_declares : a_read_model_document
{
    const string Shipping = """
        using Cratis.Chronicle.Events;

        namespace Library.Orders.Shipping;

        public record Parcel(string Label);

        [EventType]
        public record ParcelShipped(Parcel Parcel);
        """;

    const string Tracking = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Orders.Tracking;

        [ReadModel]
        public record Parcel(string Label, int Stops)
        {
            public static IEnumerable<Parcel> AllParcels() => [];
        }
        """;

    void Because() => Generate(("Library/Orders/Shipping/Shipping.cs", Shipping), ("Library/Orders/Tracking/Tracking.cs", Tracking));

    [Fact] void should_not_declare_the_read_model() => Count("readmodel Parcel").ShouldEqual(0);
    [Fact] void should_keep_the_type() => Count("type Parcel").ShouldEqual(1);
    [Fact] void should_say_why() => LeftOut.Single().ShouldContain("'Parcel' is a name the document already uses");
    [Fact] void should_compile_without_findings() => CompilationFindings.ShouldBeEmpty();
}
