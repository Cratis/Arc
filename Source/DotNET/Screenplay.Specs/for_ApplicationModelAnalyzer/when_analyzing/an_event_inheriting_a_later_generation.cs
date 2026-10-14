// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

public class an_event_inheriting_a_later_generation : Specification
{
    const string Source = """
        using Cratis.Chronicle.Events;
        namespace Library.Feature.Slice;
        [EventType("legacy-account-change", generation: 2)] public record BaseChange;
        public record InheritedChange : BaseChange;
        [EventType("replacement-account-change")] public record DirectChange : BaseChange;
        """;

    ApplicationModelAnalysis _analysis;

    void Because() => _analysis = Analyzed.Source(Source);

    EventModel Inherited => _analysis.Slice().Events.Single(_ => _.Name == "InheritedChange");
    EventModel Direct => _analysis.Slice().Events.Single(_ => _.Name == "DirectChange");

    [Fact] void should_compile_the_source() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldBeEmpty();
    [Fact] void should_not_pin_a_later_generation_as_generation_one() => Inherited.Id.ShouldBeNull();
    [Fact] void should_not_inline_the_inherited_later_generation() => Inherited.CanInline.ShouldBeFalse();
    [Fact] void should_report_the_inherited_generation_loss() => _analysis.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.EventFeatureWithoutCounterpart && _.Message.Contains("'InheritedChange' is generation 2", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_honor_a_direct_generation_one_override() => Direct.CanInline.ShouldBeTrue();
    [Fact] void should_read_the_direct_override_identifier() => Direct.Id.ShouldEqual("replacement-account-change");
}
