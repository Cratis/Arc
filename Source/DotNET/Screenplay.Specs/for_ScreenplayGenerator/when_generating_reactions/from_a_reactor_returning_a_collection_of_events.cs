// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

/// <summary>
/// A literal collection of events is appended in order to the triggering event's source, so each is a production of
/// the reaction. A scenario is only kept when it states every one of them.
/// </summary>
public class from_a_reactor_returning_a_collection_of_events : a_reacting_application
{
    const string Reactor = """
        public class Welcomer : IReactor
        {
            public IEnumerable<object> Welcome(AuthorRegistered @event) => [new AuthorWelcomed(@event.Name, "Welcome"), new AuthorFiled(@event.Country)];
        }
        """;

    const string Name = "WhenAnAuthorIsRegisteredAndTheAuthorIsWelcomed";
    const string Path = "Library/Authors/Welcoming/when_an_author_is_registered/and_the_author_is_welcomed.cs";
    const string Welcomed = "[Fact] void should_welcome_the_author() => _scenario.ShouldHaveProduced<AuthorWelcomed>(e => e.Name == \"Jane Austen\" && e.Greeting == \"Welcome\");\n";
    const string Filed = "[Fact] void should_file_the_author() => _scenario.ShouldHaveProduced<AuthorFiled>(e => e.Country == \"UK\");\n";

    static string Scenario(string assertions) => $$"""
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Testing.Reactors;
        using Library.Authors.Registration;
        using Xunit;

        namespace Library.Authors.Welcoming.when_an_author_is_registered;

        public class and_the_author_is_welcomed
        {
            readonly ReactorScenario<Welcomer> _scenario = new();
            readonly EventSourceId _author = "9c858901-8a57-4791-81fe-4c455b099bc9";

            async Task Because() => await _scenario.Given.ForEventSource(_author).Events(new AuthorRegistered("Jane Austen", "UK"));

            {{assertions}}
        }
        """;

    [Fact]
    public void should_state_every_event_and_run_the_scenario()
    {
        GenerateWith(Reactor, (Path, Scenario(Welcomed + Filed)));

        Result.Source.ShouldContain("""
                  reaction Welcomer
                    when AuthorRegistered
                      produces AuthorWelcomed
                        name = name
                        greeting = "Welcome"
                      produces AuthorFiled
                        country = country
            """);
        Result.Source.ShouldContain($"specification {Name}");
        ScenarioReports.ShouldBeEmpty();
        AssertDocument();
        var run = Run(Name);
        Assert.True(run.Passed, string.Join(Environment.NewLine, run.Failures) + Environment.NewLine + Result.Source);
    }

    [Fact]
    public void should_leave_out_a_scenario_stating_part_of_an_event()
    {
        GenerateWith(Reactor, (Path, Scenario(Welcomed.Replace(" && e.Greeting == \"Welcome\"", string.Empty, StringComparison.Ordinal) + Filed)));

        Result.Source.ShouldNotContain($"specification {Name}");
        ScenarioReports.Single().Message.ShouldContain("does not state every value of 'AuthorWelcomed'");
        AssertDocument();
    }

    [Fact]
    public void should_leave_out_a_scenario_stating_only_some_of_them()
    {
        GenerateWith(Reactor, (Path, Scenario(Welcomed)));

        Result.Source.ShouldNotContain($"specification {Name}");
        ScenarioReports.Single().Code.ShouldEqual(ScreenplayDiagnosticCodes.UnreadableSpecification);
        ScenarioReports.Single().Message.ShouldContain("does not state every event the reaction appends");
        AssertDocument();
    }
}
