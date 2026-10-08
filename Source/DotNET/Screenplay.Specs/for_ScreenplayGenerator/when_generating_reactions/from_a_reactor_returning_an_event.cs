// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

/// <summary>
/// A handler returning one event built from the triggering event is exactly a reaction producing it, and the reactor
/// scenario delivering the event is a specification appending it.
/// </summary>
public class from_a_reactor_returning_an_event : a_reacting_application
{
    const string Reactor = """
        public class Welcomer : IReactor
        {
            public AuthorWelcomed Welcome(AuthorRegistered @event, EventContext context) => new(@event.Name, "Welcome");
        }
        """;

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

            async Task Because() =>
                await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new AuthorRegistered("Jane Austen", "UK"));

            [Fact] void should_welcome_the_author() =>
                _scenario.ShouldHaveProduced<AuthorWelcomed>(e => e.Name == "Jane Austen" && e.Greeting == "Welcome");

            [Fact] void should_not_send_anything() => _scenario.ShouldNotHaveProduced<WelcomeSent>();
        }
        """;

    const string Name = "WhenAnAuthorIsRegisteredAndTheAuthorIsWelcomed";

    void Because() => GenerateWith(Reactor, ("Library/Authors/Welcoming/when_an_author_is_registered/and_the_author_is_welcomed.cs", Scenario));

    [Fact] void should_state_what_the_reaction_produces() => Result.Source.ShouldContain("""
              reaction Welcomer
                when AuthorRegistered
                  produces AuthorWelcomed
                    name = name
                    greeting = "Welcome"
        """);

    [Fact] void should_not_point_at_the_file() => Result.Source.ShouldNotContain("file Authors/Welcoming");
    [Fact] void should_state_the_scenario() => Result.Source.ShouldContain($"specification {Name}");
    [Fact] void should_append_the_delivered_event() => Result.Source.ShouldContain("when append AuthorRegistered");
    [Fact] void should_expect_what_the_reaction_appends() => Result.Source.ShouldContain("then AuthorWelcomed");
    [Fact] void should_not_report_the_scenario() => ScenarioReports.ShouldBeEmpty();
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
    [Fact] void should_pass_reference_execution() => Assert.True(Run(Name).Passed, string.Join(Environment.NewLine, Run(Name).Failures) + Environment.NewLine + Result.Source);
}
