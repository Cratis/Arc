// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

public class a_returned_event_batch : a_reacting_application
{
    protected const string ScenarioName = "WhenAnAuthorIsRegisteredAndTheAuthorIsWelcomed";
    protected const string ScenarioPath = "Library/Authors/Welcoming/when_an_author_is_registered/and_the_author_is_welcomed.cs";
    protected const string Scenario = """
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
            [Fact] void should_welcome_the_author() => _scenario.ShouldHaveProduced<AuthorWelcomed>(e => e.Name == "Jane Austen" && e.Greeting == "Welcome");
            [Fact] void should_file_the_author() => _scenario.ShouldHaveProduced<AuthorFiled>(e => e.Country == "UK");
        }
        """;

    protected static string Constraint(string definition) => $$"""
        public class FilingConstraint : Cratis.Chronicle.Events.Constraints.IConstraint
        {
            public void Define(Cratis.Chronicle.Events.Constraints.IConstraintBuilder builder) {{definition}}
        }
        """;

    protected void GenerateBatch(string events, string constraint, bool authoring = false) => Generate(
        new ScreenplayOptions { AuthoringOnlyConstructs = authoring },
        [
        ("Library/Authors/Registration/Registration.cs", Registration),
        ("Library/Authors/Welcoming/Welcoming.cs", Welcoming + $$"""
            public class Welcomer : IReactor
            {
                public IEnumerable<object> Welcome(AuthorRegistered @event) => [{{events}}];
            }
            {{constraint}}
            """),
        (IntegrationTesting.Path, IntegrationTesting.Source),
        (ScenarioPath, Scenario)
        ]);

    protected void AssertAtomicBatch(bool scenario = true)
    {
        Result.Source.ShouldContain("reaction batches atomic");
        Result.Source.ShouldNotContain("file Authors/Welcoming/Welcoming.cs");
        Result.Diagnostics.Where(_ => _.Code == "SP0070").ShouldBeEmpty();
        if (scenario)
        {
            Result.Source.ShouldContain($"specification {ScenarioName}");
            ScenarioReports.ShouldBeEmpty();
        }
        Bound.Value!.Model.SemanticVersion.ShouldEqual(Cratis.Screenplay.Semantics.SemanticVersion.V22);
        Bound.Value.Model.Application.ReactionProductionMode.ShouldEqual(Cratis.Screenplay.Semantics.SemanticReactionProductionMode.Atomic);
        AssertDocument();
    }

    protected void AssertWithheld()
    {
        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Source.ShouldNotContain($"specification {ScenarioName}");
        ScenarioReports.Single().Code.ShouldEqual(ScreenplayDiagnosticCodes.UnreadableSpecification);
        Result.Diagnostics.Single(_ => _.Code == "SP0070").Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
        AssertDocument();
    }
}
