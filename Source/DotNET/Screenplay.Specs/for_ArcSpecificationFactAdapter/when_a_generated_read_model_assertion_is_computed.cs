// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Generation;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

public class when_a_generated_read_model_assertion_is_computed : Specification
{
    const string Application = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections.ModelBound;

        namespace Projects.Projects.Overview.ListProjects;

        [EventType]
        public record ProjectRegistered(string Name, int Number);

        [Command]
        public record RegisterProject(string Name, int Number)
        {
            public ProjectRegistered Handle() => new(Name, Number);
        }

        [ReadModel]
        [FromEvent<ProjectRegistered>]
        public record ProjectOverview
        {
            public string Name { get; init; } = string.Empty;
            public int Number { get; init; }
        }
        """;

    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Testing.ReadModels;
        using Cratis.Specifications;
        using Projects.Projects.Overview.ListProjects;
        using Xunit;

        namespace Projects.Projects.Overview.ListProjects.when_a_project_was_registered;

        public class when_a_project_was_registered : Specification
        {
            readonly ReadModelScenario<ProjectOverview> _scenario = new();

            static int ExpectedNumber() => 42;

            async Task Establish() => await _scenario.Given
                .ForEventSource(EventSourceId.New())
                .Events(new ProjectRegistered("Screenplay", 42));

            [Fact] void should_project_name() => _scenario.Instance!.Name.ShouldEqual("Screenplay");
            [Fact] void should_project_number() => _scenario.Instance!.Number.ShouldEqual(ExpectedNumber());
        }
        """;

    AdapterContribution _contribution = null!;

    void Because()
    {
        var compilation = Analyzed.Compile(
        [
            ("Projects/Overview/ListProjects/ListProjects.cs", Application),
            ("Projects/Overview/ListProjects/when_a_project_was_registered.cs", Scenario),
            (IntegrationTesting.Path, IntegrationTesting.Source)
        ]);
        var project = SourceProjects.Create("Projects", DotNetProjectRole.Application, compilation);
        _contribution = new ArcSpecificationFactAdapter().Analyze(
            new DotNetAnalysisContext([project]),
            new DotNetAdapterOptions { Module = "Projects", NamespaceSegmentsToSkip = 1 });
    }

    [Fact] void should_contribute_no_partial_facts() => _contribution.Facts.ShouldBeEmpty();
    [Fact] void should_report_only_the_unsupported_scenario() => _contribution.Diagnostics.Select(_ => _.Code).ShouldContainOnly("ARCSP0001");
}
