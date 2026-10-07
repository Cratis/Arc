// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

/// <summary>
/// A read model built by a projection defined against a builder rather than with attributes, so the read model type
/// carries no projection of its own and is reached only through the projection naming it.
/// </summary>
public class a_fluent_projection : a_read_model_document
{
    const string Source = """
        using System;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections;

        namespace Library.Lending.Loans;

        [EventType]
        public record BookBorrowed(string Borrower);

        [ReadModel]
        public record Loan(Guid Id, string Borrower)
        {
            public static Loan? ById(Guid id) => null;
        }

        public class LoanProjection : IProjectionFor<Loan>
        {
            public void Define(IProjectionBuilderFor<Loan> builder) => builder.From<BookBorrowed>();
        }
        """;

    void Because() => GenerateSlice(Source);

    [Fact] void should_declare_the_read_model() => Says("readmodel Loan").ShouldBeTrue();
    [Fact] void should_declare_what_it_holds() => (Says("id Uuid") && Says("borrower String")).ShouldBeTrue();
    [Fact] void should_still_build_it_with_the_projection() => Result.Source.ShouldContain("=> Loan");
    [Fact] void should_resolve_every_reference() => UnresolvedReferences.ShouldBeEmpty();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
