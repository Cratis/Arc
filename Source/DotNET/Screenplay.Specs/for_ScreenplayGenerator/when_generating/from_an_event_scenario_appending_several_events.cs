// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A multi-event action must not be silently reduced to its first append.
/// </summary>
public class from_an_event_scenario_appending_several_events : a_generated_document
{
    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Authors.Registration;
        using Xunit;

        namespace Library.Authors.Registration.when_appending;

        public class and_there_are_two
        {
            readonly EventScenario _scenario = new();

            async Task Because() => await _scenario.When.ForEventSource(EventSourceId.New()).Events(new AuthorRegistered("First"), new AuthorRegistered("Second"));

            [Fact] Task should_append() => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>(EventSourceId.New(), e => e.Name == "First");
        }
        """;

    void Because() => Generate(
        (Analyzed.SlicePath, IdentifierSources.With("""
            [Command]
            public record RegisterAuthor(string Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            """)),
        ("Library/Feature/Slice/when_appending/and_there_are_two.cs", Scenario),
        (IntegrationTesting.Path, IntegrationTesting.Source));

    [Fact] void should_not_emit_a_partial_scenario() => Result.Source.ShouldNotContain("specification WhenAppendingAndThereAreTwo");
    [Fact] void should_report_the_unreadable_action() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification && diagnostic.Message.Contains("several events", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_call_the_scenario_kind_unrepresentable() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.ScenarioWithoutCounterpart).ShouldBeFalse();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
