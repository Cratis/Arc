// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

/// <summary>
/// A scenario asserting what a collaborator was asked to do says something about the inside of the slice that a
/// specification has nowhere to hold, even when the reactor itself is stated declaratively.
/// </summary>
public class from_a_reactor_scenario_asserting_a_collaborator : a_reacting_application
{
    const string Reactor = """
        public class Welcomer : IReactor
        {
            public AuthorWelcomed Welcome(AuthorRegistered @event) => new(@event.Name, "Welcome");
        }
        """;

    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Testing.Reactors;
        using Library.Authors.Registration;
        using Xunit;

        namespace Library.Authors.Welcoming.when_an_author_is_registered;

        public interface INotifier
        {
            void Notified(string name);
        }

        public class and_the_librarian_is_notified
        {
            readonly ReactorScenario<Welcomer> _scenario = new();
            readonly INotifier _notifier = null!;

            async Task Because() => await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new AuthorRegistered("Jane Austen", "UK"));

            [Fact] void should_welcome_the_author() => _scenario.ShouldHaveProduced<AuthorWelcomed>();

            [Fact] void should_notify_the_librarian() => _notifier.Notified("Jane Austen");
        }
        """;

    void Because() => GenerateWith(Reactor, ("Library/Authors/Welcoming/when_an_author_is_registered/and_the_librarian_is_notified.cs", Scenario));

    [Fact] void should_still_state_the_reaction() => Result.Source.ShouldContain("produces AuthorWelcomed");
    [Fact] void should_leave_the_scenario_out() => Result.Source.ShouldNotContain("specification WhenAnAuthorIsRegistered");
    [Fact] void should_report_it_without_a_counterpart() => ScenarioReports.Single().Code.ShouldEqual(ScreenplayDiagnosticCodes.ScenarioWithoutCounterpart);
    [Fact] void should_say_what_it_asserts() => ScenarioReports.Single().Message.ShouldContain("what a collaborator was asked to do");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
