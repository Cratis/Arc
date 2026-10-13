// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

public class an_event_inheriting_generation_one_metadata : Specification
{
    const string Source = """
        using Cratis.Chronicle.Events;
        namespace Library.Feature.Slice;
        [EventType("legacy-account-change")] public record BaseChange;
        public record InheritedChange : BaseChange;
        [EventType("replacement-account-change")] public record DirectChange : BaseChange;
        """;

    ApplicationModelAnalysis _analysis;

    void Because() => _analysis = Analyzed.Source(Source);

    EventModel Inherited => _analysis.Slice().Events.Single(_ => _.Name == "InheritedChange");
    EventModel Direct => _analysis.Slice().Events.Single(_ => _.Name == "DirectChange");

    [Fact] void should_compile_the_source() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldBeEmpty();
    [Fact] void should_read_the_inherited_event_identifier() => Inherited.Id.ShouldEqual("legacy-account-change");
    [Fact] void should_allow_inlining_generation_one() => Inherited.CanInline.ShouldBeTrue();
    [Fact] void should_prefer_the_nearest_direct_event_identifier() => Direct.Id.ShouldEqual("replacement-account-change");
    [Fact] void should_not_report_a_generation_loss() => _analysis.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.EventFeatureWithoutCounterpart).ShouldBeFalse();
}
