// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// An event scenario's fluent action is an append, not a command and not prior state.
/// </summary>
public class from_an_event_append_scenario : a_generated_document
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

            async Task Establish() => await _scenario.Given.ForEventSource(_source).Events(new AuthorRegistered("Prior"));

            async Task Because() => await _scenario.When.ForEventSource(_source).Events(new AuthorRegistered("Jane Austen"));

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

    [Fact] void should_omit_the_append_only_scenario() => Result.Source.ShouldNotContain("when append AuthorRegistered");
    [Fact] void should_not_assert_that_the_appended_fact_follows_itself() => Result.Source.ShouldNotContain("then AuthorRegistered");
    [Fact] void should_not_report_the_scenario_as_unrepresentable() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.ScenarioWithoutCounterpart).ShouldBeFalse();
    [Fact] void should_warn_that_no_assertion_remains() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain("assertions only restate the appended fact");
    [Fact] void should_report_a_warning() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_bind_when_the_append_assertion_is_incomplete(bool partialPredicate)
    {
        var scenario = Scenario.Replace("new AuthorRegistered(\"Prior\")", "new AuthorRegistered(\"Prior\", \"Existing\")", StringComparison.Ordinal)
            .Replace("new AuthorRegistered(\"Jane Austen\")", "new AuthorRegistered(\"Jane Austen\", \"New\")", StringComparison.Ordinal);
        if (!partialPredicate)
        {
            scenario = scenario.Replace(", e => e.Name == \"Jane Austen\"", string.Empty, StringComparison.Ordinal)
                .Replace("[Fact] Task should_append()", "[Fact] void should_append()", StringComparison.Ordinal);
        }

        Generate(
            (Analyzed.SlicePath, IdentifierSources.With("""
                [Command] public record RegisterAuthor(string Name)
                {
                    public AuthorRegistered Handle() => new(Name, "New");
                }
                """).Replace("AuthorRegistered(string Name)", "AuthorRegistered(string Name, string Status)", StringComparison.Ordinal)),
            ("Library/Feature/Slice/when_appending/and_it_succeeds.cs", scenario),
            (IntegrationTesting.Path, IntegrationTesting.Source));

        Result.Source.ShouldNotContain("when append AuthorRegistered");
        Result.Source.ShouldNotContain("then AuthorRegistered");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain("assertions only restate the appended fact");
        AssertDocument();
    }
}
