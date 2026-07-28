// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// An aggregate root states what it produces through the command that hands its work to it. One that no command
/// reaches has nothing to state it through - a document has no construct for a class that decides on its own - so
/// what it applies is reported rather than left unsaid. The slice is still a state change, because governing a change
/// is what an aggregate root is for.
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
    [Fact] void should_call_the_slice_a_state_change() => _analysis.Slice().Kind.ShouldEqual(SliceKind.StateChange);
    [Fact] void should_report_that_nothing_states_what_it_produces() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.AggregateRootWithoutCounterpart]);
    [Fact] void should_name_the_aggregate_root_in_the_report() => _analysis.Diagnostics.Single().Message.Contains("Reservation", StringComparison.Ordinal).ShouldBeTrue();
}
