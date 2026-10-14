// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

/// <summary>
/// An invocation whose successful event is constrained, with branch-only output events.
/// </summary>
public class a_refusing_application : a_reacting_application
{
    protected const string Invocation = "Invoke.Command(new SendWelcome(@event.Name, @event.Country))";
    protected const string RefusalScenarioPath = "Library/Authors/Registration/when_registering/and_a_name_is_supplied.cs";
    protected const string RefusalScenario = """
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

    protected void GenerateRefusal(string chain, bool authoring = false, bool asynchronous = false, string members = "", SemanticVersion? cap = null, string attributes = "", params (string Path, string Text)[] sources)
    {
        var returned = Invocation + chain;
        var handler = asynchronous ? $"public Task<CommandInvocation> Welcome(AuthorRegistered @event, EventContext context) => Task.FromResult({returned});" :
            $"public CommandInvocation Welcome(AuthorRegistered @event, EventContext context) => {returned};";
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring, MaximumExecutableModelVersion = cap },
        [
            ("Library/Authors/Registration/Registration.cs", Registration),
            ("Library/Authors/Welcoming/Welcoming.cs", Welcoming.Replace("WelcomeSent(string Name)", "WelcomeSent([property: Cratis.Chronicle.Events.Constraints.Unique(\"UniqueAuthor\")] string Name)", StringComparison.Ordinal) + $$"""
                {{attributes}}
                public class Welcomer : IReactor
                {
                    {{handler}}
                    {{members}}
                }
                """),
            (IntegrationTesting.Path, IntegrationTesting.Source),
            .. sources
        ]);
    }

    protected void AssertRefusalExtraction()
    {
        Result.Source.ShouldContain("invokes SendWelcome");
        Result.Source.ShouldNotContain("file Authors/Welcoming/Welcoming.cs");
        Result.Diagnostics.Where(report => report.Code == "SP0071").ShouldBeEmpty();
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V19);
        AssertDocument();
    }

    protected void AssertRefusalFallback()
    {
        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Source.ShouldNotContain("invokes SendWelcome");
        Result.Source.ShouldNotContain("on refused");
        var report = Result.Diagnostics.Single(report => report.Code == "SP0071");
        report.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
        report.Message.ShouldContain("whole trigger");
        report.Message.ShouldNotContain("not admitted");
        AssertDocument();
    }
}
