// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class from_a_filtered_reactor_scenario : a_filtered_application
{
    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Testing.Reactors;
        using Library.Authors.Registration;
        using Xunit;
        namespace Library.Authors.Welcoming.when_an_author_is_registered;
        public class and_the_author_is_welcomed
        {
            readonly ReactorScenario<Welcomer> _scenario = new();
            async Task Because() => await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new AuthorRegistered("Jane", "UK"));
            [Fact] void should_welcome_the_author() => _scenario.ShouldHaveProduced<AuthorWelcomed>(e => e.Name == "Jane" && e.Greeting == "Welcome");
        }
        """;

    void Because() => GenerateFilteredWith(
        """
        [Cratis.Chronicle.EventSources.EventSource]
        [Cratis.Chronicle.EventSources.EventStream("Onboarding")]
        public class AuthorEventSource : Cratis.Chronicle.EventSources.IEventSource;
        [Cratis.Chronicle.EventSources.FromEventSource<AuthorEventSource>("Onboarding")]
        public class Welcomer : IReactor
        {
            public AuthorWelcomed Welcome(AuthorRegistered @event) => new(@event.Name, "Welcome");
        }
        """,
        ("Library/Authors/Welcoming/when_an_author_is_registered/and_the_author_is_welcomed.cs", Scenario));

    [Fact] void should_preserve_the_filter_and_recoverable_body() => Result.Source.ShouldContain("from Author.Onboarding");
    [Fact] void should_state_the_returned_event() => Result.Source.ShouldContain("produces AuthorWelcomed");
    [Fact] void should_not_invent_a_routed_occurrence_fixture() => Result.Source.ShouldNotContain("specification WhenAnAuthorIsRegisteredAndTheAuthorIsWelcomed");
    [Fact] void should_report_the_scenario_withheld_from_execution() => ScenarioReports.Single().Code.ShouldEqual(ScreenplayDiagnosticCodes.ScenarioWithoutCounterpart);
    [Fact] void should_parse_print_and_bind_without_claiming_execution() => AssertDocument();
}
