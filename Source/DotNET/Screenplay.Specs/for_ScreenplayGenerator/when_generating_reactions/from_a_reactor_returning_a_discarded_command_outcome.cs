// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class from_a_reactor_returning_a_discarded_command_outcome : a_reacting_application
{
    [Theory]
    [InlineData("public Task Welcome(AuthorRegistered @event) => commands.Execute(new SendWelcome(@event.Name, @event.Country));")]
    [InlineData("public Task Welcome(AuthorRegistered @event) { return commands.Execute(new SendWelcome(@event.Name, @event.Country)); }")]
    [InlineData("public ValueTask Welcome(AuthorRegistered @event) => new(commands.Execute(new SendWelcome(@event.Name, @event.Country)));")]
    [InlineData("public ValueTask Welcome(AuthorRegistered @event) { return new ValueTask(commands.Execute(new SendWelcome(@event.Name, @event.Country))); }")]
    public void should_explain_that_the_forwarded_task_discards_the_result(string handler)
    {
        GenerateWith($$"""
            public class Welcomer(Cratis.Arc.Commands.ICommandPipeline commands) : IReactor
            {
                {{handler}}
            }
            """);

        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Source.ShouldNotContain("invokes SendWelcome");
        var report = Result.Diagnostics.Single(_ => _.Code == "SP0071");
        report.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
        report.Message.ShouldContain("discards the outcome");
        AssertDocument();
    }
}
