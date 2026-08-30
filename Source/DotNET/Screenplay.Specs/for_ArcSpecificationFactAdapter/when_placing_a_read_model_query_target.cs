// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Generation;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

public class when_placing_a_read_model_query_target : Specification
{
    const string StateChange = """
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

    const string StateView = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Projections.ModelBound;
        using Projects.Projects.Registration.RegisterProject;

        namespace Projects.Projects.Overview.ListProjects;

        [ReadModel]
        [FromEvent<ProjectRegistered>]
        public record ProjectOverview
        {
            public string Name { get; init; } = string.Empty;

            public static IEnumerable<ProjectOverview> AllProjects() => [];
        }
        """;

    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Testing.ReadModels;
        using Cratis.Specifications;
        using Projects.Projects.Registration.RegisterProject;
        using Xunit;

        namespace Projects.Projects.Overview.ListProjects.when_a_project_was_registered;

        public class when_a_project_was_registered : Specification
        {
            readonly ReadModelScenario<ProjectOverview> _scenario = new();

            async Task Establish() =>
                await _scenario.Given
                    .ForEventSource(EventSourceId.New())
                    .Events(new ProjectRegistered("Screenplay"));

            [Fact] void should_project_name() => _scenario.Instance!.Name.ShouldEqual("Screenplay");
        }
        """;

    AdapterContribution _contribution = null!;

    void Because()
    {
        var compilation = Analyzed.Project(
            "Projects",
            [],
            ("Source/Projects/Registration/RegisterProject/RegisterProject.cs", StateChange),
            ("Source/Projects/Overview/ListProjects/ListProjects.cs", StateView),
            ("Source/Projects/Overview/ListProjects/when_a_project_was_registered.cs", Scenario),
            (IntegrationTesting.Path, IntegrationTesting.Source));
        var project = SourceProjects.Create("Projects", DotNetProjectRole.Application, compilation);
        _contribution = new ArcSpecificationFactAdapter().Analyze(
            new DotNetAnalysisContext([project]),
            new DotNetAdapterOptions
            {
                FeatureRoot = "Source",
                Module = "Projects",
                NamespaceSegmentsToSkip = 1
            });
    }

    [Fact] void should_report_no_adapter_diagnostics() => _contribution.Diagnostics.ShouldBeEmpty();
    [Fact] void should_identify_the_read_model_target() => _contribution.Facts.OfType<SpecificationScenarioFact>().Single().Definition.TargetArtifact.Kind.ShouldEqual(ArtifactKind.ReadModel);
    [Fact] void should_place_the_read_model_and_its_query_as_a_state_view() => ReadModelPlacement().Placement.SliceKind.ShouldEqual(GenerationSliceKind.StateView);
    [Fact] void should_preserve_the_state_view_slice() => ReadModelPlacement().Placement.Slice.ShouldEqual("ListProjects");
    [Fact] void should_place_the_proven_command_event_in_its_state_change_slice() => _contribution.Facts.OfType<ArtifactPlacementFact>().Single(_ => _.Artifact.Kind == ArtifactKind.Event).Placement.Slice.ShouldEqual("RegisterProject");
    [Fact] void should_contribute_the_read_model_scenario_atomically() => _contribution.Facts.OfType<SpecificationScenarioFact>().Count().ShouldEqual(1);
    [Fact] void should_preserve_the_read_model_outcome() => _contribution.Facts.OfType<SpecificationStepFact>().Single(_ => _.Definition.Kind == SpecificationStepKind.ReadModel).Definition.Artifact!.Kind.ShouldEqual(ArtifactKind.ReadModel);

    ArtifactPlacementFact ReadModelPlacement() =>
        _contribution.Facts.OfType<ArtifactPlacementFact>().Single(_ => _.Artifact.Kind == ArtifactKind.ReadModel);
}
