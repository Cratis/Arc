// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_theory_with_a_local_scenario : a_generated_document
{
    [Theory]
    [InlineData("Cratis.Chronicle.Testing.ReadModels.ReadModelScenario<Library.Authors.Registration.AuthorRegistered>", false)]
    [InlineData("Cratis.Chronicle.Testing.ReadModels.ReadModelScenario<Library.Authors.Registration.AuthorRegistered>", true)]
    [InlineData("Cratis.Arc.Testing.Commands.CommandScenario<Library.Authors.Registration.RegisterAuthor>", false)]
    [InlineData("Cratis.Arc.Testing.Commands.CommandScenario<Library.Authors.Registration.RegisterAuthor>", true)]
    [InlineData("Cratis.Chronicle.Testing.EventSequences.EventScenario", false)]
    [InlineData("Cratis.Chronicle.Testing.EventSequences.EventScenario", true)]
    [InlineData("Cratis.Chronicle.Testing.Reactors.ReactorScenario<Library.Authors.Registration.RegisterAuthor>", false)]
    [InlineData("Cratis.Chronicle.Testing.Reactors.ReactorScenario<Library.Authors.Registration.RegisterAuthor>", true)]
    public void should_report_the_local_scenario_and_preserve_ordinary_facts(string scenarioType, bool keepFact)
    {
        var sources = CallerScenarioSources.With("", "");
        const string Theory = "[Theory] [InlineData(1)] public void should_use_local_scenario(int value) { var scenario = new Scenario(); }";
        var source = keepFact
            ? sources[^1].Text.Replace("[Fact] Task should_register()", $"{Theory}\n    [Fact] Task should_register()", StringComparison.Ordinal)
            : $$"""
                using Xunit;
                namespace Library.Authors.Registration.when_registering;
                public class with_a_caller { {{Theory}} }
                """;
        sources[^1] = (sources[^1].Path, $"using Scenario = {scenarioType};\n{source}");
        Generate(sources);
        var diagnostic = Result.Diagnostics.Single(diagnostic => diagnostic.Code == "SP0076");
        diagnostic.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
        diagnostic.Message.ShouldContain("with_a_caller.should_use_local_scenario");
        diagnostic.Location!.ShouldContain("with_a_caller.should_use_local_scenario");
        Result.Source.Contains($"specification {CallerScenarioSources.ScenarioName}", StringComparison.Ordinal).ShouldEqual(keepFact);
        AssertDocument();
    }

    [Fact]
    public void should_report_a_target_typed_local_construction()
    {
        const string Source = """
            using Xunit;
            using Cratis.Chronicle.Testing.ReadModels;
            public class when_reading
            {
                [Theory] [InlineData(1)] public void should_read(int value)
                {
                    ReadModelScenario<object> scenario = new();
                }
            }
            """;
        Generate((IntegrationTesting.Path, IntegrationTesting.Source), ("Library/when_reading.cs", Source));
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == "SP0076").Message.ShouldContain("when_reading.should_read");
        AssertDocument();
    }

    [Theory]
    [InlineData("new object()")]
    [InlineData("new ReadModelScenario<object>()")]
    [InlineData("new CommandScenario<object>()")]
    [InlineData("new EventScenario()")]
    [InlineData("new ReactorScenario<object>()")]
    public void should_ignore_plain_collaborator_theories(string construction)
    {
        Generate(("Library/when_reading.cs", $$"""
            using Xunit;
            public class ReadModelScenario<T>;
            public class CommandScenario<T>;
            public class EventScenario;
            public class ReactorScenario<T>;
            public class when_reading
            {
                [Theory] [InlineData(1)] public void should_read(int value)
                {
                    var collaborator = {{construction}};
                }
            }
            """));
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == "SP0076").ShouldBeEmpty();
        AssertDocument();
    }
}
