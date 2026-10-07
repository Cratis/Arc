// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// An event scenario without a readable action is unreadable rather than unrepresentable. A reactor scenario
/// asserting what a collaborator was asked to do still has no portable counterpart.
/// </summary>
public class a_specification_holding_a_scenario_with_no_counterpart : Specification
{
    const string Slice = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(string Name);

        [Command]
        public record RegisterAuthor(string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """;

    const string Appending = """
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Testing.EventSequences;
        using Xunit;

        namespace Library.Authors.Registration.when_claiming_a_name;

        public class and_the_name_was_already_claimed
        {
            readonly EventScenario _scenario = new();

            async Task Establish() =>
                await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new AuthorRegistered("Jane Austen"));

            void Because() => _scenario.EventSequence.ShouldHaveTailSequenceNumber(0);

            [Fact] void should_hold_one_event() => _scenario.EventSequence.ShouldHaveTailSequenceNumber(0);
        }
        """;

    const string Reacting = """
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Testing.Reactors;
        using Xunit;

        namespace Library.Authors.Registration.when_an_author_is_registered;

        public class Notifier
        {
        }

        public class and_the_librarian_is_notified
        {
            readonly ReactorScenario<Notifier> _scenario = new();

            async Task Because() =>
                await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new AuthorRegistered("Jane Austen"));

            [Fact] void should_notify_the_librarian() => "notified".ShouldEqualTheExpected("notified");
        }

        public static class Assertions
        {
            public static void ShouldEqualTheExpected(this string actual, string expected)
            {
            }
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Authors/Registration/Registration.cs", Slice),
        ("Library/Authors/Registration/when_claiming_a_name/and_the_name_was_already_claimed.cs", Appending),
        ("Library/Authors/Registration/when_an_author_is_registered/and_the_librarian_is_notified.cs", Reacting),
        (IntegrationTesting.Path, IntegrationTesting.Source)
    ];

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(_sources);

    IEnumerable<ScreenplayDiagnostic> Reported =>
        _analysis.Diagnostics.Where(_ => _.Code == ScreenplayDiagnosticCodes.ScenarioWithoutCounterpart);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_specify_the_slice_by_nothing() => _analysis.Model.Slices.Single(_ => _.Name == "Registration").Specifications.ShouldBeEmpty();
    [Fact] void should_report_every_scenario_it_left_out() => Reported.Count().ShouldEqual(1);
    [Fact] void should_report_it_as_a_warning() => Reported.All(_ => _.Severity == ScreenplayDiagnosticSeverity.Warning).ShouldBeTrue();
    [Fact] void should_report_the_event_scenario_as_unreadable_rather_than_unrepresentable() => _analysis.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.UnreadableSpecification && _.Message.Contains("and_the_name_was_already_claimed", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_name_the_scenario_that_reacts() => Reported.Any(_ => _.Message.Contains("'and_the_librarian_is_notified' is written as a ReactorScenario", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_say_where_each_one_lives() => Reported.All(_ => _.Location!.StartsWith("Library.Authors.Registration", StringComparison.Ordinal)).ShouldBeTrue();
}
