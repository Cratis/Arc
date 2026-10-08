// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

/// <summary>
/// Two read models whose names differ in C# but come out as the same declaration name. A document refers to a read
/// model by that name only, so declaring either would have the other's references resolve to it, and declaring both
/// is a duplicate the language rejects - neither is declared.
/// </summary>
public class two_read_models_declared_under_one_name : a_read_model_document
{
    const string Summaries = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Orders.Summaries;

        [ReadModel]
        public record Order_Summary(string Name)
        {
            public static IEnumerable<Order_Summary> AllSummaries() => [];
        }
        """;

    const string Totals = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Orders.Totals;

        [ReadModel]
        public record OrderSummary(int Total)
        {
            public static IEnumerable<OrderSummary> AllTotals() => [];
        }
        """;

    void Because() => Generate(("Library/Orders/Summaries/Summaries.cs", Summaries), ("Library/Orders/Totals/Totals.cs", Totals));

    [Fact] void should_declare_neither() => Count("readmodel OrderSummary").ShouldEqual(0);
    [Fact] void should_say_why_for_each() => LeftOut.Count(_ => _.Contains("'OrderSummary'")).ShouldEqual(2);
    [Fact] void should_compile_without_findings() => CompilationFindings.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => RoundTrip.IsStable.ShouldBeTrue();
}
