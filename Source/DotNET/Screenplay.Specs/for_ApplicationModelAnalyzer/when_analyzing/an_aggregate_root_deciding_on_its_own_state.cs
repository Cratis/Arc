// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A behavior refusing to act on what it has already seen is a real decision that a produces condition has nowhere
/// to put - the condition compares the input of the command, and the state an aggregate root holds is not input.
/// The production is therefore stated unconditionally and the decision is reported, so that the reader knows the
/// document is silent about it rather than that the generator missed it.
/// </summary>
public class an_aggregate_root_deciding_on_its_own_state : Specification
{
    const string Source = """
        using System.Threading.Tasks;
        using Cratis.Arc.Chronicle.Aggregates;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Lending.Reserving;

        [EventType]
        public record BookReserved(string Isbn);

        public class Reservation : AggregateRoot
        {
            bool _reserved;

            public Task Reserve(string isbn)
            {
                if (_reserved)
                {
                    return Task.CompletedTask;
                }

                return Apply(new BookReserved(isbn));
            }

            public void OnBookReserved(BookReserved @event) => _reserved = true;
        }

        [Command]
        public record ReserveBook(string Isbn)
        {
            public async Task Handle(Reservation reservation)
            {
                await reservation.Reserve(Isbn);
                await reservation.Commit();
            }
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldBeEmpty();
    [Fact] void should_still_state_what_the_command_produces() => _analysis.Slice().Commands.First().Produces.Select(_ => _.EventName).ShouldContainOnly(["BookReserved"]);
    [Fact] void should_state_it_unconditionally() => _analysis.Slice().Commands.First().Produces.First().When.ShouldBeNull();
    [Fact] void should_report_the_decision_it_could_not_carry() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.UnmappableAggregateStateCondition]);
    [Fact] void should_name_the_aggregate_root_holding_the_state() => _analysis.Diagnostics[0].Message.ShouldContain("Reservation");
}
