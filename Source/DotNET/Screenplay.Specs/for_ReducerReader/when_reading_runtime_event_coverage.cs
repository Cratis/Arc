// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Projections;

namespace Cratis.Arc.Screenplay.for_ReducerReader;

public class when_reading_runtime_event_coverage
{
    [Theory]
    [InlineData("IAccountEvent", false)]
    [InlineData("IAccountEvent", true)]
    [InlineData("UnmarkedBase", false)]
    [InlineData("UnmarkedBase", true)]
    [InlineData("ExplicitlyRegisteredEvent", false)]
    [InlineData("ExplicitlyRegisteredEvent", true)]
    public void should_recognize_the_reducer_and_report_incomplete_coverage(string eventParameter, bool includeKnown)
    {
        var source = $$"""
            #nullable enable
            using Cratis.Chronicle.Events;
            namespace Library.Feature.Slice;
            public interface IAccountEvent;
            public record UnmarkedBase;
            public record ExplicitlyRegisteredEvent : UnmarkedBase, IAccountEvent;
            [EventType] public record KnownChanged : IAccountEvent;
            [EventType] public record HelperChanged;
            public record Balance(int Total)
            {
                public static Balance? Reduce({{eventParameter}} e, Balance? current) => current;
                {{(includeKnown ? "public static Balance? Reduce(KnownChanged e, Balance? current) => current;" : string.Empty)}}
                public static void Helper(HelperChanged e) { }
            }
            """;
        var compilation = Analyzed.Compile((Analyzed.SlicePath, source));
        Analyzed.ErrorsIn(compilation).ShouldBeEmpty();
        var model = compilation.GetTypeByMetadataName("Library.Feature.Slice.Balance")!;
        ReducerReader.ReadModelOf(model).ShouldBeSame(model);
        var analysis = Analyzed.Source(source);
        var projection = analysis.Slice().Projections.Single();
        projection.ReadModel.ShouldEqual("Balance");
        string[] expected = includeKnown ? ["KnownChanged"] : [];
        projection.Scope.From.SelectMany(_ => _.EventTypes).ShouldContainOnly(expected);
        analysis.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.ReducerWithoutCounterpart &&
            _.Severity == ScreenplayDiagnosticSeverity.Warning &&
            _.Message.Contains("coverage is incomplete", StringComparison.Ordinal) &&
            _.Message.Contains(eventParameter, StringComparison.Ordinal)).ShouldBeTrue();
        analysis.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.ReducerWithoutCounterpart &&
            _.Message.Contains("nothing is said of the fold", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
