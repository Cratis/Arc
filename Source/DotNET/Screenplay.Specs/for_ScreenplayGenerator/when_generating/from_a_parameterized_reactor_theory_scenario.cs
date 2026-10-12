// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// Reactor theories report their omission without discarding ordinary facts beside them.
/// </summary>
public class from_a_parameterized_reactor_theory_scenario : a_reacting_application
{
    /// <summary>
    /// Reports both theory-only and mixed reactor scenarios.
    /// </summary>
    /// <param name="mixed">Whether the source also declares an ordinary fact.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_report_the_theory_and_preserve_only_ordinary_facts(bool mixed)
    {
        const string Reactor = """
            public class Welcomer : IReactor
            {
                public AuthorWelcomed Welcome(AuthorRegistered @event, EventContext context) => new(@event.Name, "Welcome");
            }
            """;
        var fact = mixed ? "[Fact] void should_welcome() => _scenario.ShouldHaveProduced<AuthorWelcomed>(e => e.Name == \"Jane Austen\" && e.Greeting == \"Welcome\");" : "";
        var scenario = $$"""
            using System.Threading.Tasks;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.Testing.Reactors;
            using Library.Authors.Registration;
            using Xunit;
            namespace Library.Authors.Welcoming.when_registered;
            public class with_a_theory
            {
                readonly ReactorScenario<Welcomer> _scenario = new();
                async Task Because() => await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new AuthorRegistered("Jane Austen", "UK"));
                [Theory] [InlineData("Jane Austen")] void should_welcome_each_author(string name) => _scenario.ShouldHaveProduced<AuthorWelcomed>(e => e.Name == name && e.Greeting == "Welcome");
                {{fact}}
            }
            """;
        GenerateWith(Reactor, ("Library/Authors/Welcoming/when_registered/with_a_theory.cs", scenario));
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == "SP0076").Message.ShouldContain("with_a_theory.should_welcome_each_author");
        Result.Source.Contains("specification WhenRegisteredWithATheory", StringComparison.Ordinal).ShouldEqual(mixed);
        AssertDocument();
    }
}
