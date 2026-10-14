// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

public class a_self_reducing_read_model : Specification
{
    const string Source = """
        #nullable enable
        using System.Threading.Tasks;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        namespace Library.Feature.Slice;
        [EventType] public record Added(int Amount);
        public record ExtraAdded(int Amount) : Added(Amount);
        [EventType] public record Removed;
        [EventType] public record Ignored;
        [ReadModel] public record Balance(int Total)
        {
            public static Balance Reduce(Added e, Balance? current) => new((current?.Total ?? 0) + e.Amount);
            public static Task<Balance?> Reduce(Removed e, Balance? current, EventContext context) => Task.FromResult<Balance?>(null);
            public static Balance Reduce(ExtraAdded e, Balance? current) => new(e.Amount);
            public static void Helper(Ignored e) { }
            public static string Reduce(Ignored e, string unrelated) => unrelated;
            public static Balance All() => new(0);
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Because() => _analysis = Analyzed.Source(Source);

    ProjectionModel Projection => _analysis.Slice().Projections.Single();

    [Fact] void should_compile_the_source() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldBeEmpty();
    [Fact] void should_recover_the_self_model() => Projection.ReadModel.ShouldEqual("Balance");
    [Fact] void should_observe_only_eligible_reductions_including_inherited_event_markers() => Projection.Scope.From.SelectMany(_ => _.EventTypes).ShouldContainOnly("Added", "ExtraAdded", "Removed");
    [Fact] void should_not_invent_fold_mappings() => Projection.Scope.From.SelectMany(_ => _.Properties).ShouldBeEmpty();
    [Fact] void should_report_the_fold_loss() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.ReducerWithoutCounterpart);
    [Fact] void should_keep_queries_without_exposing_self_reductions() => _analysis.Slice().Queries.Select(_ => _.Name).ShouldNotContain("Reduce");
}
