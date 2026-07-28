// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// The same decision written as a conditional expression has to read the same way, and the branch taken when the
/// condition does not hold has to carry the opposite condition rather than none - otherwise both events would look
/// as if they were always produced.
/// </summary>
public class a_handler_choosing_with_a_conditional_expression : Specification
{
    const string Source = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Lending.Reserving;

        [EventType]
        public record BookReserved(string Isbn);

        [EventType]
        public record ReservationRefused(string Isbn);

        [Command]
        public record ReserveBook(string Isbn, bool InStock)
        {
            public object Handle() => InStock ? new BookReserved(Isbn) : (object)new ReservationRefused(Isbn);
        }
        """;

    ApplicationModelAnalysis _analysis;
    IEnumerable<ProducesModel> _produces;

    void Establish()
    {
        _analysis = Analyzed.Source(Source);
        _produces = _analysis.Slice().Commands.First().Produces;
    }

    ProducesModel Produced(string name) => _produces.First(_ => _.EventName == name);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_produce_both_events() => _produces.Select(_ => _.EventName).ShouldContainOnly(["BookReserved", "ReservationRefused"]);
    [Fact] void should_guard_the_branch_taken_when_it_holds() => Produced("BookReserved").When.ShouldEqual(new ComparisonCondition("InStock", ComparisonKind.Equal, new LiteralSource(true)));
    [Fact] void should_guard_the_other_branch_with_the_opposite() => Produced("ReservationRefused").When.ShouldEqual(new ComparisonCondition("InStock", ComparisonKind.NotEqual, new LiteralSource(true)));
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
