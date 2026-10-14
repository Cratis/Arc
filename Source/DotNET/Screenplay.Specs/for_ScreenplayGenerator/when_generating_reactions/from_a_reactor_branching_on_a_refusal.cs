// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class from_a_reactor_branching_on_a_refusal : a_returned_event_batch
{
    [Theory]
    [InlineData("var result = await commands.Execute(new SendWelcome(@event.Name, @event.Country)); if (!result.IsSuccess) throw new ApplicationException();", false)]
    [InlineData("var result = await commands.Execute(new SendWelcome(@event.Name, @event.Country)); if (!result.IsValid) return;", true)]
    [InlineData("var result = await commands.Execute(new SendWelcome(@event.Name, @event.Country)); if (result is { IsAuthorized: false }) return;", false)]
    [InlineData("_ = await commands.Execute(new SendWelcome(@event.Name, @event.Country));", true)]
    [InlineData("await commands.Execute(new SendWelcome(@event.Name, @event.Country));", false)]
    public void should_explain_outcome_handling_without_stating_an_invocation(string body, bool authoring)
    {
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring },
        [
            ("Library/Authors/Registration/Registration.cs", Registration),
            ("Library/Authors/Welcoming/Welcoming.cs", Welcoming + $$"""
                public class Welcomer(Cratis.Arc.Commands.ICommandPipeline commands) : IReactor
                {
                    public async Task Welcome(AuthorRegistered @event) { {{body}} }
                }
                """),
            (IntegrationTesting.Path, IntegrationTesting.Source),
            (ScenarioPath, Scenario)
        ]);

        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Source.ShouldNotContain("invokes SendWelcome");
        var report = Result.Diagnostics.Single(_ => _.Code == "SP0071");
        report.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
        report.Location.ShouldEqual("Library.Authors.Welcoming.Welcomer");
        report.Message.ShouldContain("cannot be proved equivalent");
        report.Message.ShouldNotContain("not admitted");
        report.Message.ShouldNotContain("Arc#3058");
        Result.Diagnostics.Where(_ => _.Code == "SP0072").ShouldBeEmpty();
        ScenarioReports.Single().Code.ShouldEqual(ScreenplayDiagnosticCodes.ScenarioWithoutCounterpart);
        AssertDocument();
    }

    [Fact]
    public void should_not_report_result_inspection_without_pipeline_execution()
    {
        GenerateWith("""
            public class Welcomer : IReactor
            {
                public void Welcome(AuthorRegistered @event)
                {
                    var result = new Cratis.Arc.Commands.CommandResult();
                    if (!result.IsSuccess) return;
                }
            }
            """);

        Result.Diagnostics.Where(_ => _.Code == "SP0071" || _.Code == "SP0072").ShouldBeEmpty();
        AssertDocument();
    }
}
