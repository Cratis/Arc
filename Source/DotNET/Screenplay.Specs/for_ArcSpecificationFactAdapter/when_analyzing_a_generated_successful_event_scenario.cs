// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Generation;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;
using Cratis.Screenplay.Printing;

namespace Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

public class when_analyzing_a_generated_successful_event_scenario : Specification
{
    const string Slice = """
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

    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Specifications;
        using Projects.Projects.Registration.RegisterProject;
        using Xunit;

        namespace Projects.Projects.Registration.RegisterProject.when_registering;

        public class when_registering : Specification
        {
            readonly CommandScenario<RegisterProject> _scenario = new();
            CommandResult _result = null!;

            async Task Because() => _result = await _scenario.Execute(new RegisterProject("Screenplay", 42));

            [Fact] void should_succeed() => _result.ShouldBeSuccessful();
            [Fact] async Task should_append() => await _scenario.ShouldHaveAppendedEvent<RegisterProject, ProjectRegistered>(
                "project",
                @event => @event.Name == "Screenplay" &&
                          @event.Number == 42);
        }
        """;

    const string Assertions = """
        using System;
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.Events;

        namespace Cratis.Arc.Chronicle.Testing.Commands;

        public static class CommandScenarioChronicleAssertionExtensions
        {
            public static Task ShouldHaveAppendedEvent<TCommand, TEvent>(
                this CommandScenario<TCommand> scenario,
                EventSourceId eventSourceId,
                Func<TEvent, bool> predicate) => Task.CompletedTask;
        }
        """;

    AdapterContribution _contribution = null!;
    AdapterContribution _reversed = null!;
    ResolvedApplicationGraph _graph = null!;
    ScreenplayLoweringResult _lowering = null!;
    string _source = null!;
    string _reversedSource = null!;
    string _reversedAdapterSource = null!;
    string _reversedFactSource = null!;

    void Because()
    {
        _contribution = AnalyzeSeparateProjects(reverseProjects: false, reverseSyntaxTrees: false);
        _reversed = AnalyzeSeparateProjects(reverseProjects: true, reverseSyntaxTrees: true);
        _graph = new GenerationResolver().Resolve([_contribution]);
        _lowering = new ScreenplayLowerer().Lower(_graph, "Projects");
        var empty = new AdapterContribution
        {
            Adapter = new AdapterIdentity { Id = "test.empty", Version = "1.0.0" }
        };
        _source = Source([_contribution, empty]);
        _reversedSource = Source([_reversed, empty]);
        _reversedAdapterSource = Source([empty, _contribution]);
        _reversedFactSource = Source([_contribution with { Facts = [.. _contribution.Facts.Reverse()] }, empty]);
    }

    [Fact] void should_have_no_adapter_diagnostics() => _contribution.Diagnostics.ShouldBeEmpty();
    [Fact] void should_contribute_one_scenario() => _contribution.Facts.OfType<SpecificationScenarioFact>().Count().ShouldEqual(1);
    [Fact] void should_contribute_the_ordered_command_and_event_steps() => _contribution.Facts.OfType<SpecificationStepFact>().OrderBy(_ => _.Definition.Key.Index).Select(_ => (_.Definition.Phase, _.Definition.Kind)).ShouldEqual([(SpecificationStepPhase.When, SpecificationStepKind.Command), (SpecificationStepPhase.Then, SpecificationStepKind.Event)]);
    [Fact] void should_contribute_the_exact_command_and_event_values() => _contribution.Facts.OfType<SpecificationValueFact>().OrderBy(_ => _.Definition.Key.Step.Index).ThenBy(_ => _.Definition.Key.Path.Single(), StringComparer.Ordinal).Select(_ => (_.Definition.Key.Path.Single(), _.Definition.Scalar)).ShouldEqual([("Name", "Screenplay"), ("Number", "42"), ("Name", "Screenplay"), ("Number", "42")]);
    [Fact] void should_contribute_the_command_and_event_artifacts() => _contribution.Facts.OfType<ArtifactFact>().Select(_ => _.Definition.Key.Kind).ShouldContainOnly([ArtifactKind.Command, ArtifactKind.Event]);
    [Fact] void should_contribute_exact_command_and_event_placements() => _contribution.Facts.OfType<ArtifactPlacementFact>().Count().ShouldEqual(2);
    [Fact] void should_place_both_artifacts_in_the_exact_slice() => _contribution.Facts.OfType<ArtifactPlacementFact>().All(_ => _.Placement.Slice == "RegisterProject").ShouldBeTrue();
    [Fact] void should_preserve_exact_scenario_step_and_value_source_evidence() => _contribution.Facts.Where(_ => _ is SpecificationScenarioFact or SpecificationStepFact or SpecificationValueFact).All(_ => _.Evidence.Source?.Path == "Projects/Registration/RegisterProject/when_registering.cs").ShouldBeTrue();
    [Fact] void should_preserve_the_exact_scenario_source_range() => Range(_contribution.Facts.OfType<SpecificationScenarioFact>().Single()).ShouldEqual((10, 14, 10, 30));
    [Fact] void should_preserve_the_exact_command_step_source_range() => Range(_contribution.Facts.OfType<SpecificationStepFact>().Single(_ => _.Definition.Kind == SpecificationStepKind.Command)).ShouldEqual((15, 45, 15, 101));
    [Fact] void should_preserve_the_exact_command_value_source_ranges() => _contribution.Facts.OfType<SpecificationValueFact>().Where(_ => _.Definition.Key.Step.Index == 0).OrderBy(_ => _.Definition.Key.Path.Single(), StringComparer.Ordinal).Select(Range).ShouldEqual([(15, 83, 15, 95), (15, 97, 15, 99)]);
    [Fact] void should_preserve_the_exact_event_step_source_range() => Range(_contribution.Facts.OfType<SpecificationStepFact>().Single(_ => _.Definition.Kind == SpecificationStepKind.Event)).ShouldEqual((18, 48, 21, 39));
    [Fact] void should_preserve_the_exact_event_value_source_ranges() => _contribution.Facts.OfType<SpecificationValueFact>().Where(_ => _.Definition.Key.Step.Index == 1).OrderBy(_ => _.Definition.Key.Path.Single(), StringComparer.Ordinal).Select(Range).ShouldEqual([(20, 34, 20, 46), (21, 36, 21, 38)]);
    [Fact] void should_resolve_one_atomic_scenario() => _graph.Specifications.Count.ShouldEqual(1);
    [Fact] void should_attach_the_scenario_to_the_exact_slice() => _graph.Specifications.Single().Placement.Slice.ShouldEqual("RegisterProject");
    [Fact] void should_preserve_the_scenario_name() => _graph.Specifications.Single().Definition.Name.ShouldEqual("when_registering_when_registering");
    [Fact] void should_lower_without_diagnostics() => _lowering.Diagnostics.ShouldBeEmpty();
    [Fact] void should_lower_the_exact_command_values() => Lowered().When!.Values.OrderBy(_ => _.Property, StringComparer.Ordinal).Select(_ => ((Cratis.Screenplay.Syntax.LiteralExpressionSyntax)_.Source).Value).ShouldEqual(["Screenplay", 42m]);
    [Fact] void should_lower_the_exact_event_values() => Lowered().ThenEvents.Single().Values.OrderBy(_ => _.Property, StringComparer.Ordinal).Select(_ => ((Cratis.Screenplay.Syntax.LiteralExpressionSyntax)_.Source).Value).ShouldEqual(["Screenplay", 42m]);
    [Fact] void should_be_independent_of_project_and_syntax_tree_order() => _reversedSource.ShouldEqual(_source);
    [Fact] void should_preserve_identical_fact_and_source_evidence_under_project_and_tree_reversal() => FactEvidence(_reversed).ShouldEqual(FactEvidence(_contribution));
    [Fact] void should_be_independent_of_adapter_order() => _reversedAdapterSource.ShouldEqual(_source);
    [Fact] void should_be_independent_of_neutral_fact_order() => _reversedFactSource.ShouldEqual(_source);

    static AdapterContribution AnalyzeSeparateProjects(bool reverseProjects, bool reverseSyntaxTrees)
    {
        var application = Analyzed.Project(
            "Projects",
            [],
            ("Projects/Registration/RegisterProject/RegisterProject.cs", Slice));
        var specificationSources = new[]
        {
            ("Projects/Registration/RegisterProject/when_registering.cs", Scenario),
            ("Integration/Assertions.cs", Assertions),
            (IntegrationTesting.Path, IntegrationTesting.Source)
        };
        if (reverseSyntaxTrees)
        {
            specificationSources = [.. specificationSources.AsEnumerable().Reverse()];
        }

        var specifications = Analyzed.Project(
            "Projects.Specifications",
            [application.ToMetadataReference()],
            specificationSources);
        DotNetProjectCompilation[] projects =
        [
            SourceProjects.Create("Projects", DotNetProjectRole.Application, application),
            SourceProjects.Create("Projects.Specifications", DotNetProjectRole.Specifications, specifications)
        ];
        if (reverseProjects)
        {
            projects = [.. projects.AsEnumerable().Reverse()];
        }

        return new ArcSpecificationFactAdapter().Analyze(
            new DotNetAnalysisContext(projects),
            new DotNetAdapterOptions { Module = "Projects", NamespaceSegmentsToSkip = 1 });
    }

    static string Source(IReadOnlyList<AdapterContribution> contributions)
    {
        var graph = new GenerationResolver().Resolve(contributions);
        var lowering = new ScreenplayLowerer().Lower(graph, "Projects");
        return new ScreenplayPrinter().Print(lowering.Application);
    }

    static string[] FactEvidence(AdapterContribution contribution) =>
    [
        .. contribution.Facts
            .OrderBy(_ => _.Id.Value, StringComparer.Ordinal)
            .Select(_ => $"{_.Id.Value}|{_.Evidence.Source?.Path}:{_.Evidence.Source?.StartLine}:{_.Evidence.Source?.StartColumn}:{_.Evidence.Source?.EndLine}:{_.Evidence.Source?.EndColumn}")
    ];

    Cratis.Screenplay.Syntax.Specifications.SpecificationSyntax Lowered() =>
        _lowering.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single();

    static (int StartLine, int StartColumn, int EndLine, int EndColumn) Range(GenerationFact fact) =>
        (fact.Evidence.Source!.StartLine, fact.Evidence.Source.StartColumn, fact.Evidence.Source.EndLine, fact.Evidence.Source.EndColumn);
}
