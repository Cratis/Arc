// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// Appending directly to an event scenario's sequence states the same action as its fluent builder.
/// </summary>
public class from_an_event_sequence_append_scenario : a_generated_document
{
    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Authors.Registration;
        using Xunit;

        namespace Library.Authors.Registration.when_appending;

        public class and_it_succeeds
        {
            readonly EventScenario _scenario = new();
            readonly EventSourceId _source = EventSourceId.New();

            async Task Because() => await _scenario.EventSequence.Append(_source, new AuthorRegistered("Jane Austen"));

            [Fact] Task should_append() => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>(_source, e => e.Name == "Jane Austen");
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
        ("Library/Feature/Slice/when_appending/and_it_succeeds.cs", Scenario),
        (IntegrationTesting.Path, IntegrationTesting.Source));

    [Fact] void should_state_the_append_action() => Result.Source.ShouldContain("when append AuthorRegistered");
    [Fact] void should_not_drop_the_scenario() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeFalse();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
