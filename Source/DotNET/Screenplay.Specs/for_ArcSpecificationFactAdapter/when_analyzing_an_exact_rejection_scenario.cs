// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Generation;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

public class when_analyzing_an_exact_rejection_scenario : Specification
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
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Projects.Projects.Registration.RegisterProject;
        using Xunit;

        namespace Projects.Projects.Registration.RegisterProject.when_rejecting_an_empty_project_name;

        public class when_rejecting_an_empty_project_name
        {
            readonly CommandScenario<RegisterProject> _scenario = new();
            Result _result = null!;

            async Task Because() => _result = await _scenario.Execute(new RegisterProject(""));

            [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
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
            ("Projects/Registration/RegisterProject/RegisterProject.cs", Slice),
            ("Projects/Registration/RegisterProject/when_rejecting_an_empty_project_name.cs", Scenario),
            (IntegrationTesting.Path, IntegrationTesting.Source)
        ]);
        var project = new DotNetProjectCompilation
        {
            Name = "Projects",
            Compilation = compilation,
            AuthoredSyntaxTrees = compilation.SyntaxTrees.ToHashSet()
        };
        var context = new DotNetAnalysisContext([project]);
        var adapter = new ArcSpecificationFactAdapter();
        _canAnalyze = adapter.CanAnalyze(context);
        _contribution = adapter.Analyze(
            context,
            new DotNetAdapterOptions { Module = "Projects", NamespaceSegmentsToSkip = 1 });
        _graph = new GenerationResolver().Resolve([_contribution]);
        _lowering = new ScreenplayLowerer().Lower(_graph, "Projects");
    }

    [Fact] void should_recognize_the_scenario() => _canAnalyze.ShouldBeTrue();
    [Fact] void should_have_no_adapter_diagnostics() => _contribution.Diagnostics.ShouldBeEmpty();
    [Fact] void should_contribute_one_scenario_fact() => _contribution.Facts.OfType<SpecificationScenarioFact>().Count().ShouldEqual(1);
    [Fact] void should_contribute_the_when_and_error_steps() => _contribution.Facts.OfType<SpecificationStepFact>().Count().ShouldEqual(2);
    [Fact] void should_contribute_the_exact_empty_value() => _contribution.Facts.OfType<SpecificationValueFact>().Single().Definition.Scalar.ShouldEqual(string.Empty);
    [Fact] void should_contribute_one_atomic_scenario() => _graph.Specifications.Count.ShouldEqual(1);
    [Fact] void should_attach_the_scenario_to_the_exact_slice() => _graph.Specifications.Single().Placement.Slice.ShouldEqual("RegisterProject");
    [Fact] void should_preserve_the_when_command() => _graph.Specifications.Single().Steps.Single(step => step.Definition.Phase == SpecificationStepPhase.When).Definition.Kind.ShouldEqual(SpecificationStepKind.Command);
    [Fact] void should_preserve_the_empty_name_value() => _graph.Specifications.Single().Steps.SelectMany(step => step.Values).Single().Definition.Scalar.ShouldEqual(string.Empty);
    [Fact] void should_preserve_the_rejected_outcome() => _graph.Specifications.Single().Steps.Single(step => step.Definition.Kind == SpecificationStepKind.Error).Definition.ErrorMessage.ShouldBeNull();
    [Fact] void should_preserve_step_level_source_evidence() => _graph.Specifications.Single().Steps.All(step => step.Evidence.Single().Source is not null).ShouldBeTrue();
    [Fact] void should_lower_without_diagnostics() => _lowering.Diagnostics.ShouldBeEmpty();
    [Fact] void should_lower_the_exact_command_value() => ((Cratis.Screenplay.Syntax.LiteralExpressionSyntax)Lowered().When!.Values.Single().Source).Value.ShouldEqual(string.Empty);
    [Fact] void should_lower_the_bare_rejection_without_inventing_a_message() => Lowered().ThenErrors.Single().Name.ShouldBeNull();

    Cratis.Screenplay.Syntax.Specifications.SpecificationSyntax Lowered() =>
        _lowering.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single();
}
