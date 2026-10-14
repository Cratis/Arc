// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class from_a_reactor_handling_refusals_fluently : a_reacting_application
{
    [Theory]
    [InlineData("Refused.ByValidation")]
    [InlineData("Refused.ByAuthorization")]
    [InlineData("Refused.ByAnyConstraint")]
    [InlineData("Refused.ByConstraint(\"UniqueAuthor\")")]
    [InlineData("Refused.Any")]
    public void should_keep_each_selector_file_backed_in_both_modes(string selector)
    {
        foreach (var authoring in new[] { false, true })
        {
            foreach (var synchronous in new[] { false, true })
            {
                var invocation = $"Invoke.Command(new SendWelcome(@event.Name, @event.Country)).OnRefused({selector}, refusal => new AuthorFiled(refusal.Message))";
                var handler = synchronous ? $"public CommandInvocation Welcome(AuthorRegistered @event) => {invocation};" :
                    $"public Task<CommandInvocation> Welcome(AuthorRegistered @event) => Task.FromResult({invocation});";
                GenerateReaction(handler, authoring);
                AssertRefusalFallback();
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_recognize_acknowledgment(bool authoring)
    {
        GenerateReaction("public CommandInvocation Welcome(AuthorRegistered @event) => Invoke.Command(new SendWelcome(@event.Name, @event.Country)).Acknowledge(Refused.ByValidation);", authoring);
        AssertRefusalFallback();
    }

    [Fact]
    public void should_not_recognize_a_lookalike_branch_method()
    {
        GenerateWith("""
            public class Welcomer : IReactor
            {
                public object Welcome(AuthorRegistered @event) => new Lookalike().OnRefused(Refused.Any, _ => null);
            }
            public class Lookalike
            {
                public object OnRefused(Refused selector, Func<Cratis.Arc.Commands.CommandRefusal, object?> handler) => this;
            }
            """);

        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Diagnostics.Where(_ => _.Code == "SP0071").ShouldBeEmpty();
        AssertDocument();
    }

    void GenerateReaction(string handler, bool authoring) => Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring },
    [
        ("Library/Authors/Registration/Registration.cs", Registration),
        ("Library/Authors/Welcoming/Welcoming.cs", Welcoming + $$"""
            public class Welcomer : IReactor
            {
                {{handler}}
            }
            """)
    ]);

    void AssertRefusalFallback()
    {
        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Source.ShouldNotContain("invokes SendWelcome");
        Result.Source.ShouldNotContain("on refused");
        var report = Result.Diagnostics.Single(_ => _.Code == "SP0071");
        report.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
        report.Message.ShouldContain("Screenplay#433");
        report.Message.ShouldNotContain("Arc#3058");
        Result.Diagnostics.Where(_ => _.Code == "SP0072").ShouldBeEmpty();
        AssertDocument();
    }
}
