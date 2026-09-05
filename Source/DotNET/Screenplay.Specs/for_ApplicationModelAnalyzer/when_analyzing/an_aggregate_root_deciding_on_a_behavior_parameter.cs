// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A behavior decides between two events on a value the handler passed it, and the aggregate root named that value
/// whatever it liked. Without the substitution the call site declares, the decision would look like code and both
/// events would be stated as always produced - which describes an application that emits both every time.
/// </summary>
public class an_aggregate_root_deciding_on_a_behavior_parameter : Specification
{
    const string Source = """
        using System.Threading.Tasks;
        using Cratis.Arc.Chronicle.Aggregates;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Lending.Reserving;

        [EventType]
        public record BookReserved(string Isbn);

        [EventType]
        public record ReservationRefused(string Isbn);

        public class Reservation : AggregateRoot
        {
            public Task Reserve(string isbn, int copies)
            {
                if (copies > 0)
                {
                    return Apply(new BookReserved(isbn));
                }

                return Apply(new ReservationRefused(isbn));
            }

            public void OnBookReserved(BookReserved @event)
            {
            }

            public void OnReservationRefused(ReservationRefused @event)
            {
            }
        }

        [Command]
        public record ReserveBook(string Isbn, int CopiesWanted)
        {
            public async Task Handle(Reservation reservation)
            {
                await reservation.Reserve(Isbn, CopiesWanted);
                await reservation.Commit();
            }
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    ConditionModel? ConditionFor(string @event) =>
        _analysis.Slice().Commands.First().Produces.First(_ => _.EventName == @event).When;

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldBeEmpty();
    [Fact] void should_state_both_events() => _analysis.Slice().Commands.First().Produces.Select(_ => _.EventName).ShouldContainOnly(["BookReserved", "ReservationRefused"]);
    [Fact] void should_resolve_the_guard_back_to_the_command_input() => ConditionFor("BookReserved").ShouldEqual(new ComparisonCondition("CopiesWanted", ComparisonKind.GreaterThan, new LiteralSource(0)));
    [Fact] void should_invert_the_guard_for_the_other_outcome() => ConditionFor("ReservationRefused").ShouldEqual(new ComparisonCondition("CopiesWanted", ComparisonKind.LessThanOrEqual, new LiteralSource(0)));
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
