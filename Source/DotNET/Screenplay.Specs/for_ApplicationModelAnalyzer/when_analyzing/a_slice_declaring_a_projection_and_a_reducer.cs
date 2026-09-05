// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A reducer builds a read model of its own, so what is recovered from it stands beside the projections of the slice
/// rather than in place of one. A slice keeping a single projection made the two compete, and which read model
/// survived was decided by the order the compilation happened to be walked in - so an application mixing the two
/// shapes, which is the ordinary one, lost read models it really builds and lost different ones as it grew.
/// </summary>
public class a_slice_declaring_a_projection_and_a_reducer : Specification
{
    const string Source = """
        using System.Threading.Tasks;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections.ModelBound;
        using Cratis.Chronicle.Reducers;

        namespace Library.Lending.Balances;

        [EventType]
        public record BookReserved(string Isbn);

        [EventType]
        public record BookReturned(string Isbn);

        [ReadModel]
        public record Balance(int Outstanding);

        public class BalanceReducer : IReducerFor<Balance>
        {
            public Task<Balance> Reserved(BookReserved @event, Balance? current, EventContext context) =>
                Task.FromResult(new Balance((current?.Outstanding ?? 0) + 1));
        }

        [ReadModel]
        [FromEvent<BookReserved>]
        public record Reservation
        {
            [SetFrom<BookReserved>("isbn")]
            public string Isbn { get; init; } = string.Empty;
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_carry_both_read_models() => _analysis.Slice().Projections.Select(_ => _.ReadModel).ShouldContainOnly(["Balance", "Reservation"]);
    [Fact] void should_leave_neither_out() => _analysis.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.UnmappableProjectionConstruct).ShouldBeFalse();
    [Fact] void should_still_report_the_fold_as_lost() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.ReducerWithoutCounterpart);
}
