// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Generation;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

public class when_analyzing_a_generated_read_model_scenario : Specification
{
    const string Event = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Projects.Projects.Registration.RegisterProject;

        [EventType]
        public record ProjectRegistered(string Name, int Number);

        [Command]
        public record RegisterProject(string Name, int Number)
        {
            public ProjectRegistered Handle() => new(Name, Number);
        }
        """;

    const string StateView = """
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Projections.ModelBound;
        using Projects.Projects.Registration.RegisterProject;

        namespace Projects.Projects.Overview.ListProjects;

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
        using Projects.Projects.Registration.RegisterProject;
        using Xunit;

        namespace Projects.Projects.Overview.ListProjects.when_a_project_was_registered;

        public class when_a_project_was_registered : Specification
        {
            readonly EventSourceId _projectId = EventSourceId.New();
            readonly ReadModelScenario<ProjectOverview> _scenario = new();

            async Task Establish() => await _scenario.Given
                .ForEventSource(_projectId)
                .Events(new ProjectRegistered("Screenplay", 42));

            [Fact] void should_project_name() => _scenario.Instance!.Name.ShouldEqual("Screenplay");
            [Fact] void should_project_number() => _scenario.Instance!.Number.ShouldEqual(42);
        }
        """;

    AdapterContribution _contribution = null!;
    ResolvedApplicationGraph _graph = null!;
    ScreenplayLoweringResult _lowering = null!;
    bool _canAnalyze;

    void Because()
    {
        var compilation = Analyzed.Compile(
        [
            ("Projects/Registration/RegisterProject/ProjectRegistered.cs", Event),
            ("Projects/Overview/ListProjects/ProjectOverview.cs", StateView),
            ("Projects/Overview/ListProjects/when_a_project_was_registered.cs", Scenario),
            (IntegrationTesting.Path, IntegrationTesting.Source)
        ]);
        var project = SourceProjects.Create("Projects", DotNetProjectRole.Application, compilation);
        var context = new DotNetAnalysisContext([project]);
        var adapter = new ArcSpecificationFactAdapter();
        _canAnalyze = adapter.CanAnalyze(context);
        _contribution = adapter.Analyze(
            context,
            new DotNetAdapterOptions { Module = "Projects", NamespaceSegmentsToSkip = 1 });
        _graph = new GenerationResolver().Resolve([_contribution]);
        _lowering = new ScreenplayLowerer().Lower(_graph, "Projects");
    }

    [Fact] void should_recognize_the_generated_scenario() => _canAnalyze.ShouldBeTrue();
    [Fact] void should_have_no_adapter_diagnostics() => _contribution.Diagnostics.ShouldBeEmpty();
    [Fact] void should_contribute_one_scenario() => _contribution.Facts.OfType<SpecificationScenarioFact>().Count().ShouldEqual(1);
    [Fact] void should_contribute_the_ordered_given_event_and_then_read_model() => _contribution.Facts.OfType<SpecificationStepFact>().OrderBy(_ => _.Definition.Key.Index).Select(_ => (_.Definition.Phase, _.Definition.Kind)).ShouldEqual([(SpecificationStepPhase.Given, SpecificationStepKind.Event), (SpecificationStepPhase.Then, SpecificationStepKind.ReadModel)]);
    [Fact] void should_contribute_every_exact_event_and_read_model_value() => _contribution.Facts.OfType<SpecificationValueFact>().Count().ShouldEqual(4);
    [Fact] void should_contribute_the_event_and_read_model_artifacts() => _contribution.Facts.OfType<ArtifactFact>().Select(_ => _.Definition.Key.Kind).ShouldContainOnly([ArtifactKind.Event, ArtifactKind.ReadModel]);
    [Fact] void should_place_the_event_and_read_model_in_their_exact_slices() => _contribution.Facts.OfType<ArtifactPlacementFact>().Select(_ => (_.Placement.Slice, _.Placement.SliceKind)).ShouldContainOnly([("RegisterProject", GenerationSliceKind.StateChange), ("ListProjects", GenerationSliceKind.StateView)]);
    [Fact] void should_preserve_the_exact_scenario_source_range() => Range(_contribution.Facts.OfType<SpecificationScenarioFact>().Single()).ShouldEqual((11, 14, 11, 43));
    [Fact] void should_preserve_the_exact_given_event_source_range() => Range(_contribution.Facts.OfType<SpecificationStepFact>().Single(_ => _.Definition.Kind == SpecificationStepKind.Event)).ShouldEqual((18, 17, 18, 56));
    [Fact] void should_preserve_the_exact_given_event_value_ranges() => _contribution.Facts.OfType<SpecificationValueFact>().Where(_ => _.Definition.Key.Step.Index == 0).OrderBy(_ => _.Definition.Key.Path.Single(), StringComparer.Ordinal).Select(Range).ShouldEqual([(18, 39, 18, 51), (18, 53, 18, 55)]);
    [Fact] void should_preserve_the_exact_read_model_source_range() => Range(_contribution.Facts.OfType<SpecificationStepFact>().Single(_ => _.Definition.Kind == SpecificationStepKind.ReadModel)).ShouldEqual((14, 32, 14, 47));
    [Fact] void should_preserve_the_exact_read_model_value_ranges() => _contribution.Facts.OfType<SpecificationValueFact>().Where(_ => _.Definition.Key.Step.Index == 1).OrderBy(_ => _.Definition.Key.Path.Single(), StringComparer.Ordinal).Select(Range).ShouldEqual([(20, 79, 20, 91), (21, 83, 21, 85)]);
    [Fact] void should_resolve_one_atomic_scenario() => _graph.Specifications.Count.ShouldEqual(1);
    [Fact] void should_lower_without_diagnostics() => _lowering.Diagnostics.ShouldBeEmpty();
    [Fact] void should_lower_no_when_step() => Lowered().When.ShouldBeNull();
    [Fact] void should_lower_the_given_event() => Lowered().Given.Single().EventType.ShouldEqual("ProjectRegistered");
    [Fact] void should_lower_every_read_model_value() => Lowered().ThenReadModels.Single().Properties.Count().ShouldEqual(2);

    Cratis.Screenplay.Syntax.Specifications.SpecificationSyntax Lowered() =>
        _lowering.Application.Modules.SelectMany(_ => _.Features).SelectMany(_ => _.Slices).Single(_ => _.Specifications.Any()).Specifications.Single();

    static (int StartLine, int StartColumn, int EndLine, int EndColumn) Range(GenerationFact fact) =>
        (fact.Evidence.Source!.StartLine, fact.Evidence.Source.StartColumn, fact.Evidence.Source.EndLine, fact.Evidence.Source.EndColumn);
}
