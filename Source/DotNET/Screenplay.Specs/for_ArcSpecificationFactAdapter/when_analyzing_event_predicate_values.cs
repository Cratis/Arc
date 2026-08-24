// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Generation;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

public class when_analyzing_event_predicate_values : Specification
{
    const string Slice = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Projects.Projects.Registration.RegisterProject;

        [EventType]
        public record ProjectRegistered(string Name);

        [Command]
        public record RegisterProject(string Name)
        {
            public ProjectRegistered Handle() => new(Name);
        }
        """;

    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Arc.Testing.Commands;
        using Projects.Projects.Registration.RegisterProject;
        using Xunit;

        namespace Projects.Projects.Registration.RegisterProject.when_registering;

        public class when_registering
        {
            readonly CommandScenario<RegisterProject> _scenario = new();

            async Task Because() => await _scenario.Execute(new RegisterProject("Screenplay"));

            [Fact] Task should_append() => _scenario.ShouldHaveAppendedEvent<RegisterProject, ProjectRegistered>(
                "project",
                @event => @event.Name == "Screenplay");
        }
        """;

    const string Assertions = """
        using System;
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.Events;

        namespace Cratis.Arc.Chronicle.Testing.Commands;

        public static class Assertions
        {
            public static Task ShouldHaveAppendedEvent<TCommand, TEvent>(
                this CommandScenario<TCommand> scenario,
                EventSourceId eventSourceId,
                Func<TEvent, bool> predicate) => Task.CompletedTask;
        }
        """;

    AdapterContribution _contribution = null!;

    void Because()
    {
        var compilation = Analyzed.Compile(
        [
            ("Projects/Registration/RegisterProject/RegisterProject.cs", Slice),
            ("Projects/Registration/RegisterProject/when_registering.cs", Scenario),
            ("Integration/Assertions.cs", Assertions),
            (IntegrationTesting.Path, IntegrationTesting.Source)
        ]);
        var project = new DotNetProjectCompilation
        {
            Name = "Projects",
            Compilation = compilation,
            AuthoredSyntaxTrees = compilation.SyntaxTrees.ToHashSet()
        };
        _contribution = new ArcSpecificationFactAdapter().Analyze(
            new DotNetAnalysisContext([project]),
            new DotNetAdapterOptions { Module = "Projects", NamespaceSegmentsToSkip = 1 });
    }

    [Fact] void should_contribute_no_partial_scenario() => _contribution.Facts.OfType<SpecificationScenarioFact>().ShouldBeEmpty();
    [Fact] void should_report_the_unrepresented_predicate_values() => _contribution.Diagnostics.Single().Code.ShouldEqual("ARCSP0001");
    [Fact] void should_retain_the_scenario_source() => _contribution.Diagnostics.Single().Source!.Path.ShouldEqual("Projects/Registration/RegisterProject/when_registering.cs");
}
