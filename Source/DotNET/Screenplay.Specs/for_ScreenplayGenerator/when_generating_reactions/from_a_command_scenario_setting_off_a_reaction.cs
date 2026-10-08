// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

/// <summary>
/// A command scenario in Arc runs no reactor, while a specification states every fact that follows - the reactions'
/// as well. A scenario whose facts set off a declarative reaction would state less than the document runs, so it is
/// left out; when the reaction is code it is kept exactly as before.
/// </summary>
public class from_a_command_scenario_setting_off_a_reaction : a_reacting_application
{
    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Authors.Registration;
        using Xunit;

        namespace Library.Authors.Registration.when_registering;

        public class and_a_name_is_supplied
        {
            readonly CommandScenario<RegisterAuthor> _scenario = new();
            async Task Because() => await _scenario.Execute(new RegisterAuthor("jane", "Jane Austen", "UK"));

            [Fact] Task should_register_the_author() => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>("jane", e => e.Name == "Jane Austen" && e.Country == "UK");
        }
        """;

    const string Name = "WhenRegisteringAndANameIsSupplied";

    [Fact]
    public void should_leave_it_out_when_the_reaction_is_declarative()
    {
        GenerateWith(
            "public class Welcomer : IReactor { public AuthorWelcomed Welcome(AuthorRegistered @event) => new(@event.Name, \"Welcome\"); }",
            ("Library/Authors/Registration/when_registering/and_a_name_is_supplied.cs", Scenario));

        Result.Source.ShouldNotContain($"specification {Name}");
        ScenarioReports.Single().Message.ShouldContain("sets off the declarative reaction 'Welcomer'");
        AssertDocument();
    }

    [Fact]
    public void should_keep_it_when_the_reaction_is_code()
    {
        GenerateWith(
            "public class Welcomer : IReactor { public void Welcome(AuthorRegistered @event) { } }",
            ("Library/Authors/Registration/when_registering/and_a_name_is_supplied.cs", Scenario));

        Result.Source.ShouldContain($"specification {Name}");
        ScenarioReports.ShouldBeEmpty();
        AssertDocument();
    }
}
