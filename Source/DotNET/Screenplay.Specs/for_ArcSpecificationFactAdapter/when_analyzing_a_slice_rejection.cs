// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Generation;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

public class when_analyzing_a_slice_rejection : Specification
{
    AdapterContribution _contribution = null!;
    ResolvedApplicationGraph _graph = null!;
    bool _canAnalyze;

    void Because()
    {
        var sources = SliceRejectionSources.With();
        Analyzed.ErrorsIn(sources).ShouldBeEmpty();
        var project = SourceProjects.Create("Library", DotNetProjectRole.Application, Analyzed.Compile(sources));
        var context = new DotNetAnalysisContext([project]);
        var adapter = new ArcSpecificationFactAdapter();
        _canAnalyze = adapter.CanAnalyze(context);
        _contribution = adapter.Analyze(context, new DotNetAdapterOptions { Module = "Library", NamespaceSegmentsToSkip = 1 });
        _graph = new GenerationResolver().Resolve([_contribution]);
    }

    [Fact] void should_discover_the_held_slice() => _canAnalyze.ShouldBeTrue();
    [Fact] void should_not_report_a_gap() => _contribution.Diagnostics.ShouldBeEmpty();
    [Fact] void should_retain_one_scenario() => _graph.Specifications.Count.ShouldEqual(1);
    [Fact] void should_target_the_command_slice() => _graph.Specifications.Single().Placement.Slice.ShouldEqual("Registration");
    [Fact] void should_retain_the_command() => _graph.Specifications.Single().Steps.Single(step => step.Definition.Phase == SpecificationStepPhase.When).Definition.Kind.ShouldEqual(SpecificationStepKind.Command);
    [Fact] void should_retain_the_constructor_command_value() => _contribution.Facts.OfType<SpecificationValueFact>().Single().Definition.Scalar.ShouldEqual(string.Empty);
    [Fact] void should_retain_an_unnamed_rejection() => _graph.Specifications.Single().Steps.Single(step => step.Definition.Kind == SpecificationStepKind.Error).Definition.ErrorMessage.ShouldBeNull();
    [Fact] void should_retain_exact_step_sources() => _graph.Specifications.Single().Steps.All(step => step.Evidence.Single().Source is not null).ShouldBeTrue();
}
