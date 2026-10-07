// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models;

/// <summary>
/// A read model holding a record whose simple name a different record an event carries already uses. The event's
/// record is declared under the name first, so the read model's property would be written with a name declaring the
/// event's shape - the read model is not declared, and the event's record is declared exactly as before.
/// </summary>
public class a_read_model_holding_a_record_named_like_one_an_event_carries : a_read_model_document
{
    const string Ordering = """
        using System.Collections.Generic;
        using Cratis.Chronicle.Events;

        namespace Library.Orders.Ordering;

        public record Line(string Product, int Quantity);

        [EventType]
        public record OrderPlaced(IEnumerable<Line> Lines);
        """;

    const string Reporting = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Orders.Reporting;

        public record Line(decimal Amount);

        [ReadModel]
        public record Report(string Title, IEnumerable<Line> Lines)
        {
            public static IEnumerable<Report> AllReports() => [];
        }
        """;

    void Because() => Generate(("Library/Orders/Ordering/Ordering.cs", Ordering), ("Library/Orders/Reporting/Reporting.cs", Reporting));

    [Fact] void should_not_declare_the_read_model() => Count("readmodel Report").ShouldEqual(0);
    [Fact] void should_declare_the_events_record_once() => Count("type Line").ShouldEqual(1);
    [Fact] void should_keep_the_events_shape() => Says("product String").ShouldBeTrue();
    [Fact] void should_say_which_property() => LeftOut.Single().ShouldContain("'Lines'");
    [Fact] void should_report_no_warning() => GenerationWarnings.ShouldBeEmpty();
    [Fact] void should_compile_without_findings() => CompilationFindings.ShouldBeEmpty();
}
