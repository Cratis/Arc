// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Generations, tombstones and compensations are all first class in Chronicle and have no counterpart in Screenplay
/// at all. Inventing syntax for them would be worse than saying nothing, so each is reported once for the event
/// declaring it and the event is otherwise recovered as usual.
/// </summary>
public class an_event_with_no_screenplay_counterpart : Specification
{
    const string Source = """
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        [EventType(generation: 2)]
        public record AuthorRegistered(string Name);

        [EventType]
        [Tombstone]
        public record AuthorForgotten(string Name);

        [EventType]
        [CompensationFor<AuthorRegistered>]
        public record AuthorRegistrationReversed(string Name);
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    IEnumerable<string> Reported => _analysis.Diagnostics.Select(_ => _.Message);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_still_declare_every_event() => _analysis.Slice().Events.Select(_ => _.Name).ShouldContainOnly(["AuthorRegistered", "AuthorForgotten", "AuthorRegistrationReversed"]);
    [Fact] void should_report_all_three_losses() => _analysis.Diagnostics.Count.ShouldEqual(3);
    [Fact] void should_report_only_the_one_code() => _analysis.Diagnostics.Select(_ => _.Code).Distinct(StringComparer.Ordinal).ShouldContainOnly([ScreenplayDiagnosticCodes.EventFeatureWithoutCounterpart]);
    [Fact] void should_say_which_generation_was_lost() => Reported.Any(_ => _.Contains("generation 2", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_say_the_tombstone_was_lost() => Reported.Any(_ => _.Contains("tombstone", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_say_the_compensation_was_lost() => Reported.Any(_ => _.Contains("compensates", StringComparison.Ordinal)).ShouldBeTrue();
}
