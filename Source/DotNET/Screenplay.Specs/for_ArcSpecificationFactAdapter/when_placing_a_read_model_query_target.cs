// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Generation;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

public class when_placing_a_read_model_query_target : Specification
{
    const string StateView = """
        using System.Collections.Generic;
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

            public static IEnumerable<ProjectOverview> AllProjects() => [];
        }
        """;

    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Testing.ReadModels;

        namespace Projects.Projects.Overview.ListProjects.when_a_project_was_registered;

        public class when_a_project_was_registered
        {
            readonly ReadModelScenario<ProjectOverview> _scenario = new();

            async Task Because() =>
                await _scenario.Given
                    .ForEventSource(EventSourceId.New())
                    .Events(new ProjectRegistered("Screenplay"));
        }
        """;

    AdapterContribution _contribution = null!;

    void Because()
    {
        var compilation = Analyzed.Project(
            "Projects",
            [],
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
    [Fact] void should_place_the_read_model_and_its_query_as_a_state_view() => _contribution.Facts.OfType<ArtifactPlacementFact>().Single().Placement.SliceKind.ShouldEqual(GenerationSliceKind.StateView);
    [Fact] void should_preserve_the_state_view_slice() => _contribution.Facts.OfType<ArtifactPlacementFact>().Single().Placement.Slice.ShouldEqual("ListProjects");
    [Fact] void should_contribute_the_read_model_scenario_atomically() => _contribution.Facts.OfType<SpecificationScenarioFact>().Count().ShouldEqual(1);
    [Fact] void should_preserve_the_read_model_outcome() => _contribution.Facts.OfType<SpecificationStepFact>().Single(_ => _.Definition.Kind == SpecificationStepKind.ReadModel).Definition.Artifact!.Kind.ShouldEqual(ArtifactKind.ReadModel);
}
