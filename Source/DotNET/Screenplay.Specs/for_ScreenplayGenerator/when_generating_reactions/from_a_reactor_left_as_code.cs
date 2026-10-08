// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

/// <summary>
/// A handler that decides, computes a value, or does anything beyond returning what it constructs from the event is
/// code. It keeps the file reference it had before, and a scenario of it has no counterpart.
/// </summary>
public class from_a_reactor_left_as_code : a_reacting_application
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

            async Task Because() => await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new AuthorRegistered("Jane Austen", "UK"));

            [Fact] void should_welcome_the_author() => _scenario.ShouldHaveProduced<AuthorWelcomed>();
        }
        """;

    [Theory]
    [InlineData("public IEnumerable<object> Welcome(AuthorRegistered @event) { if (@event.Country == \"UK\") return [new AuthorWelcomed(@event.Name, \"Welcome\")]; return []; }")]
    [InlineData("public IEnumerable<object> Welcome(AuthorRegistered @event) => @event.Country == \"UK\" ? [new AuthorWelcomed(@event.Name, \"Welcome\")] : [];")]
    [InlineData("public AuthorWelcomed Welcome(AuthorRegistered @event) => new(@event.Name.ToUpperInvariant(), \"Welcome\");")]
    [InlineData("public AuthorWelcomed Welcome(AuthorRegistered @event) => new(@event.Name, $\"Welcome {@event.Name}\");")]
    [InlineData("public AuthorWelcomed Welcome(AuthorRegistered @event) { Console.WriteLine(@event.Name); return new(@event.Name, \"Welcome\"); }")]
    [InlineData("public AuthorWelcomed Welcome(AuthorRegistered @event, IServiceProvider services) => new(@event.Name, \"Welcome\");")]
    [InlineData("public AuthorWelcomed Welcome(AuthorRegistered @event) => new(@event.Name, Greeting); string Greeting => \"Welcome\";")]
    [InlineData("public AuthorWelcomed Welcome(AuthorRegistered @event, EventContext context) => new(context.EventSourceId.Value, \"Welcome\");")]
    [InlineData("public object Welcome(AuthorRegistered @event) => new AuthorWelcomed(@event.Name, \"Welcome\");")]
    public void should_keep_pointing_at_the_file(string handler)
    {
        GenerateWith($$"""public class Welcomer : IReactor { {{handler}} }""", ("Library/Authors/Welcoming/when_an_author_is_registered/and_the_author_is_welcomed.cs", Scenario));

        Result.Source.ShouldContain("""
                  reaction Welcomer
                    when AuthorRegistered
                      file Authors/Welcoming/Welcoming.cs
            """);
        Result.Source.ShouldNotContain("produces AuthorWelcomed");
        Result.Source.ShouldNotContain("specification WhenAnAuthorIsRegistered");
        ScenarioReports.Single().Code.ShouldEqual(ScreenplayDiagnosticCodes.ScenarioWithoutCounterpart);
        ScenarioReports.Single().Message.ShouldContain("handlers are code");
        AssertDocument();
    }

    [Fact]
    public void should_keep_a_reactor_choosing_its_event_source_as_code()
    {
        GenerateWith("""
            public class Welcomer : IReactor, ICanProvideEventSourceId
            {
                public EventSourceId GetEventSourceId() => "welcomes";
                public AuthorWelcomed Welcome(AuthorRegistered @event) => new(@event.Name, "Welcome");
            }
            """);

        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Source.ShouldNotContain("produces AuthorWelcomed");
        AssertDocument();
    }
}
