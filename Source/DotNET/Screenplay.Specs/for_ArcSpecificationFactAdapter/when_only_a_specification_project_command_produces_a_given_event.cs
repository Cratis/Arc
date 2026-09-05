// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Generation;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

public class when_only_a_specification_project_command_produces_a_given_event : Specification
{
    const string Application = """
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections.ModelBound;

        namespace Projects.Projects.Overview.ListProjects;

        [EventType]
        public record ProjectRegistered(string Name);

        [ReadModel]
        [FromEvent<ProjectRegistered>]
        public record ProjectOverview
        {
            public string Name { get; init; } = string.Empty;
        }
        """;

    const string FixtureCommand = """
        using Cratis.Arc.Commands.ModelBound;
        using Projects.Projects.Overview.ListProjects;

        namespace Projects.Specifications.Fixtures;

        [Command]
        public record FixtureRegisterProject(string Name)
        {
            public ProjectRegistered Handle() => new(Name);
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

            async Task Establish() => await _scenario.Given
                .ForEventSource(EventSourceId.New())
                .Events(new ProjectRegistered("Screenplay"));

            [Fact] void should_project_name() => _scenario.Instance!.Name.ShouldEqual("Screenplay");
        }
        """;

    AdapterContribution _contribution = null!;

    void Because()
    {
        var application = Analyzed.Project(
            "Projects",
            [],
            ("Projects/Overview/ListProjects/ListProjects.cs", Application));
        var specifications = Analyzed.Project(
            "Projects.Specifications",
            [application.ToMetadataReference()],
            ("Fixtures/FixtureRegisterProject.cs", FixtureCommand),
            ("Projects/Overview/ListProjects/when_a_project_was_registered.cs", Scenario),
            (IntegrationTesting.Path, IntegrationTesting.Source));
        var context = new DotNetAnalysisContext(
        [
            SourceProjects.Create("Projects", DotNetProjectRole.Application, application),
            SourceProjects.Create("Projects.Specifications", DotNetProjectRole.Specifications, specifications)
        ]);
        _contribution = new ArcSpecificationFactAdapter().Analyze(
            context,
            new DotNetAdapterOptions { Module = "Projects", NamespaceSegmentsToSkip = 1 });
    }

    [Fact] void should_contribute_no_partial_facts() => _contribution.Facts.ShouldBeEmpty();
    [Fact] void should_report_only_the_unproven_event_placement() => _contribution.Diagnostics.Select(_ => _.Code).ShouldContainOnly("ARCSP0001");
}
