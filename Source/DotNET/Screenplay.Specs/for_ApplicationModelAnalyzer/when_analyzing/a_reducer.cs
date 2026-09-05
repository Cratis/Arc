// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A reducer folds events into a read model with code, and Screenplay has no construct for a fold. The events it
/// observes and the read model it builds are still worth stating, so those are recovered and the fold itself is
/// reported as lost rather than silently invented.
/// </summary>
public class a_reducer : Specification
{
    const string Source = """
        using System.Threading.Tasks;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
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

            public Task<Balance> Returned(BookReturned @event, Balance? current, EventContext context) =>
                Task.FromResult(new Balance((current?.Outstanding ?? 0) - 1));
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    ProjectionModel Projection => _analysis.Slice().Projections.Single();

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_still_state_the_read_model_it_builds() => Projection.ReadModel.ShouldEqual("Balance");
    [Fact] void should_still_state_the_events_it_observes() => Projection.Scope.From.SelectMany(_ => _.EventTypes).ShouldContainOnly(["BookReserved", "BookReturned"]);
    [Fact] void should_state_no_mappings_since_the_fold_is_code() => Projection.Scope.From.SelectMany(_ => _.Properties).ShouldBeEmpty();
    [Fact] void should_report_the_fold_as_lost() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.ReducerWithoutCounterpart);
}
