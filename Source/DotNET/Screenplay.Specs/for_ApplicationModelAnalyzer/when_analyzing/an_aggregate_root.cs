// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// An aggregate root decides what happened in code, applying events from inside its own methods. A document says what
/// a command produces declaratively, and there is no honest way to state a decision that lives in a class. Saying so
/// is the difference between a document that is incomplete and one that is quietly wrong.
/// </summary>
public class an_aggregate_root : Specification
{
    const string Source = """
        using System.Threading.Tasks;
        using Cratis.Arc.Chronicle.Aggregates;
        using Cratis.Chronicle.Events;

        namespace Library.Lending.Reserving;

        [EventType]
        public record BookReserved(string Isbn);

        public class Reservation : AggregateRoot
        {
            public Task Reserve(string isbn) => Apply(new BookReserved(isbn));

            public void On(BookReserved @event)
            {
            }
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_still_declare_the_event_it_applies() => _analysis.Slice().Events.Single().Name.ShouldEqual("BookReserved");
    [Fact] void should_report_that_what_it_produces_is_not_stated() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.AggregateRootWithoutCounterpart]);
    [Fact] void should_name_the_aggregate_root_in_the_report() => _analysis.Diagnostics.Single().Message.Contains("Reservation", StringComparison.Ordinal).ShouldBeTrue();
}
