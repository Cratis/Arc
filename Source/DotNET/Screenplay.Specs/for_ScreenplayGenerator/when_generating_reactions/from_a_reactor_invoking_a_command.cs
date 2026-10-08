// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

/// <summary>
/// A command a reactor hands back through a task, or in a collection of objects, is executed by Arc's command side
/// effect handlers - exactly what a reaction invoking it says. A scenario asserting the invoked command rather than
/// the facts it records has no counterpart.
/// </summary>
public class from_a_reactor_invoking_a_command : a_reacting_application
{
    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Testing.Reactors;
        using Library.Authors.Registration;
        using Xunit;

        namespace Library.Authors.Welcoming.when_an_author_is_registered;

        public class and_a_welcome_is_sent
        {
            readonly ReactorScenario<Welcomer> _scenario = new();

            async Task Because() =>
                await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new AuthorRegistered("Jane Austen", "UK"));

            [Fact] void should_send_a_welcome() => _scenario.ShouldHaveProduced<SendWelcome>(e => e.Name == "Jane Austen");
        }
        """;

    [Theory]
    [InlineData("public Task<SendWelcome> Welcome(AuthorRegistered @event) => Task.FromResult(new SendWelcome(@event.Name, @event.Country));")]
    [InlineData("public async Task<SendWelcome> Welcome(AuthorRegistered @event) { await Task.Yield(); return new SendWelcome(@event.Name, @event.Country); }")]
    [InlineData("public IEnumerable<object> Welcome(AuthorRegistered @event) => [new SendWelcome(@event.Name, @event.Country)];")]
    public void should_state_what_the_reaction_invokes(string handler)
    {
        GenerateWith($$"""public class Welcomer : IReactor { {{handler}} }""", ("Library/Authors/Welcoming/when_an_author_is_registered/and_a_welcome_is_sent.cs", Scenario));

        if (handler.Contains("Task.Yield", StringComparison.Ordinal))
        {
            Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
            Result.Source.ShouldNotContain("invokes SendWelcome");
        }
        else
        {
            Result.Source.ShouldContain("""
                      reaction Welcomer
                        when AuthorRegistered
                          invokes SendWelcome
                            name = name
                            country = country
                """);
            Result.Source.ShouldNotContain("file Authors/Welcoming/Welcoming.cs");
        }

        AssertDocument();
    }

    [Fact]
    public void should_leave_out_the_scenario_asserting_the_invoked_command()
    {
        GenerateWith(
            "public class Welcomer : IReactor { public Task<SendWelcome> Welcome(AuthorRegistered @event) => Task.FromResult(new SendWelcome(@event.Name, @event.Country)); }",
            ("Library/Authors/Welcoming/when_an_author_is_registered/and_a_welcome_is_sent.cs", Scenario));

        Result.Source.ShouldNotContain("specification WhenAnAuthorIsRegisteredAndAWelcomeIsSent");
        ScenarioReports.Single().Code.ShouldEqual(ScreenplayDiagnosticCodes.ScenarioWithoutCounterpart);
        ScenarioReports.Single().Message.ShouldContain("invokes 'SendWelcome'");
        AssertDocument();
    }

    [Theory]
    [InlineData("public SendWelcome Welcome(AuthorRegistered @event) => new(@event.Name, @event.Country);")]
    [InlineData("[ExecuteCommandsAsSystem(\"Librarian\")] public class Welcomer : IReactor { public Task<SendWelcome> Welcome(AuthorRegistered @event) => Task.FromResult(new SendWelcome(@event.Name, @event.Country)); }")]
    public void should_keep_a_command_the_runtime_does_not_execute_as_a_reaction_without_a_caller_as_code(string declaration)
    {
        GenerateWith(declaration.StartsWith('[') ? declaration : $$"""public class Welcomer : IReactor { {{declaration}} }""");

        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Source.ShouldNotContain("invokes SendWelcome");
        AssertDocument();
    }
}
