// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Generation;
using Cratis.Screenplay;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

public class when_analyzing_a_generated_query_specification : Specification
{
    AdapterContribution _sameProject = null!;
    bool _canAnalyze;
    AdapterContribution _separateProject = null!;
    AdapterContribution _siblingProject = null!;
    AdapterContribution _reversed = null!;
    AdapterContribution _relocated = null!;
    AdapterContribution _withoutEstablish = null!;
    AdapterContribution _partial = null!;
    AdapterContribution _partialReversed = null!;
    AdapterContribution _concepts = null!;
    ResolvedApplicationGraph _graph = null!;
    ScreenplayLoweringResult _lowering = null!;
    CompilationResult<Cratis.Screenplay.Syntax.ApplicationSyntax> _compiled = null!;
    string _source = null!;
    string _reprinted = null!;
    string _sameProjectSource = null!;
    string _siblingProjectSource = null!;
    string _reversedSource = null!;
    string _relocatedSource = null!;
    string _withoutEstablishSource = null!;
    string _partialSource = null!;
    string _partialReversedSource = null!;
    string _reversedFactsSource = null!;
    string _reversedAdaptersSource = null!;

    void Because()
    {
        _canAnalyze = CanAnalyze();
        _sameProject = Analyze(ProjectShape.Same, false, false, string.Empty);
        _separateProject = Analyze(ProjectShape.Separate, false, false, string.Empty);
        _siblingProject = Analyze(ProjectShape.Sibling, false, false, string.Empty);
        _reversed = Analyze(ProjectShape.Separate, true, true, string.Empty);
        _relocated = Analyze(ProjectShape.Separate, false, false, "/another/checkout/");
        _withoutEstablish = Analyze(
            ProjectShape.Separate,
            false,
            false,
            string.Empty,
            GeneratedQuerySpecificationSources.Scenario.Replace(
                "\n    void Establish() => _readModels.GetInstanceById<ProjectOverview>((EventSourceId)new ProjectId(Guid.Parse(\"f5a0c7ef-2e5f-4c4d-8d25-62b477b75f4e\"))).Returns(_expected);\n",
                string.Empty,
                StringComparison.Ordinal));
        _partial = AnalyzePartial(reverseTrees: false);
        _partialReversed = AnalyzePartial(reverseTrees: true);
        _concepts = Concepts(_separateProject);
        _graph = new GenerationResolver().Resolve([_separateProject, _concepts]);
        _lowering = new ScreenplayLowerer().Lower(_graph, "Projects");
        _source = new ScreenplayPrinter().Print(_lowering.Application);
        _compiled = new ScreenplayCompiler().Compile(_source);
        _reprinted = _compiled.Value is null ? string.Empty : new ScreenplayPrinter().Print(_compiled.Value);
        _sameProjectSource = Source([_sameProject]);
        _siblingProjectSource = Source([_siblingProject]);
        _reversedSource = Source([_reversed]);
        _relocatedSource = Source([_relocated]);
        _withoutEstablishSource = Source([_withoutEstablish]);
        _partialSource = Source([_partial]);
        _partialReversedSource = Source([_partialReversed]);
        _reversedFactsSource = Source([_separateProject with { Facts = [.. _separateProject.Facts.Reverse()] }]);
        _reversedAdaptersSource = SourceRaw([_concepts, _separateProject]);
    }

    [Fact] void should_recognize_an_application_query_with_a_specification_shaped_type() => _canAnalyze.ShouldBeTrue();
    [Fact] void should_report_no_adapter_diagnostics() => _separateProject.Diagnostics.ShouldBeEmpty();
    [Fact] void should_emit_only_the_complete_query_recovery_facts() => _separateProject.Facts.Count.ShouldEqual(13);
    [Fact] void should_emit_the_query_and_read_model_artifacts() => _separateProject.Facts.OfType<ArtifactFact>().Select(_ => _.Definition.Key.Kind).ShouldContainOnly([ArtifactKind.Query, ArtifactKind.ReadModel]);
    [Fact] void should_identify_the_query_by_its_exact_method_subject() => QueryArtifact().Subject.Value.ShouldContain("#method:");
    [Fact] void should_not_treat_the_query_source_file_as_a_performer() => QueryArtifact().Definition.File.ShouldBeNull();
    [Fact] void should_mark_the_single_required_query_input_as_the_identifier() => QueryArtifact().Definition.Properties.Single().IsIdentifier.ShouldBeTrue();
    [Fact] void should_align_the_query_key_with_the_exact_read_model_property() => (QueryArtifact().Definition.Properties.Single().Name, ReadModelArtifact().Definition.Properties.Single(_ => _.Name == "projectId").Name).ShouldEqual(("projectId", "projectId"));
    [Fact] void should_emit_exact_supported_query_result_types() => ReadModelArtifact().Definition.Properties.Select(_ => _.Type.Name).ShouldEqual(["ProjectId", "ProjectName", "Int", "Date", "DateTime"]);
    [Fact] void should_emit_the_exact_returns_relationship() => Returns().Definition.Key.ShouldEqual(new RelationshipKey { Kind = RelationshipKind.Returns, Source = QueryArtifact().Subject, Target = ReadModelArtifact().Subject });
    [Fact] void should_preserve_the_optional_single_result_flags() => (Returns().Definition.IsOptional, Returns().Definition.IsCollection).ShouldEqual((true, false));
    [Fact] void should_target_the_scenario_at_the_query() => Scenario().Definition.TargetArtifact.ShouldEqual(QueryArtifact().Definition.Key);
    [Fact] void should_emit_one_then_read_step() => (Step().Definition.Phase, Step().Definition.Kind).ShouldEqual((SpecificationStepPhase.Then, SpecificationStepKind.Read));
    [Fact] void should_emit_no_given_or_when_step() => _separateProject.Facts.OfType<SpecificationStepFact>().Any(_ => _.Definition.Phase is SpecificationStepPhase.Given or SpecificationStepPhase.When).ShouldBeFalse();
    [Fact] void should_preserve_formal_argument_and_property_order() => Step().Definition.Values.Select(_ => string.Join('/', _.Path)).ShouldEqual(["arguments/projectId", "result/0/projectId", "result/0/name", "result/0/number", "result/0/startedOn", "result/0/updatedAt"]);
    [Fact] void should_preserve_the_exact_concept_and_scalar_values() => Step().Definition.Values.Select(key => Value(key).Definition.Scalar).ShouldEqual(["f5a0c7ef-2e5f-4c4d-8d25-62b477b75f4e", "f5a0c7ef-2e5f-4c4d-8d25-62b477b75f4e", "Screenplay", "42", "2026-02-14", "2026-02-14T10:15:30+00:00"]);
    [Fact] void should_place_the_query_and_read_model_together_as_a_state_view() => _separateProject.Facts.OfType<ArtifactPlacementFact>().Select(_ => (_.Placement.Slice, _.Placement.SliceKind)).ShouldContainOnly([("ListProjects", GenerationSliceKind.StateView), ("ListProjects", GenerationSliceKind.StateView)]);
    [Fact] void should_resolve_one_atomic_scenario() => _graph.Specifications.Count.ShouldEqual(1);
    [Fact] void should_resolve_without_diagnostics() => _graph.Diagnostics.ShouldBeEmpty();
    [Fact] void should_lower_without_diagnostics() => _lowering.Diagnostics.ShouldBeEmpty();
    [Fact] void should_lower_the_query_without_a_when() => Lowered().When.ShouldBeNull();
    [Fact] void should_lower_the_query_without_a_performer() => LoweredQuery().Performer.ShouldBeNull();
    [Fact] void should_print_the_query_without_a_performer() => _source.ShouldNotContain("performer");
    [Fact] void should_lower_the_exact_query_argument() => Lowered().ThenQueries.Single().Arguments.Single().Property.ShouldEqual("projectId");
    [Fact] void should_lower_one_expected_snapshot() => Lowered().ThenQueries.Single().Results.Count().ShouldEqual(1);
    [Fact] void should_compile_the_printed_document() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_reprint_byte_identically() => _reprinted.ShouldEqual(_source);
    [Fact] void should_be_identical_when_query_and_specification_share_a_project() => _sameProjectSource.ShouldEqual(_source);
    [Fact] void should_be_identical_with_a_sibling_framework_project() => _siblingProjectSource.ShouldEqual(_source);
    [Fact] void should_be_independent_of_project_and_syntax_tree_order() => _reversedSource.ShouldEqual(_source);
    [Fact] void should_be_independent_of_fact_order() => _reversedFactsSource.ShouldEqual(_source);
    [Fact] void should_be_independent_of_adapter_order() => _reversedAdaptersSource.ShouldEqual(_source);
    [Fact] void should_be_independent_of_checkout_location() => _relocatedSource.ShouldEqual(_source);
    [Fact] void should_allow_the_corroborating_establish_to_be_absent() => _withoutEstablish.Diagnostics.ShouldBeEmpty();
    [Fact] void should_recover_the_same_scenario_without_corroborating_establish() => _withoutEstablishSource.ShouldEqual(_source);
    [Fact] void should_recover_the_complete_partial_scenario() => _partial.Diagnostics.ShouldBeEmpty();
    [Fact] void should_be_independent_of_partial_syntax_tree_order() => _partialReversedSource.ShouldEqual(_partialSource);
    [Fact] void should_preserve_partial_scenario_evidence_when_reversed() => FactEvidence(_partialReversed).ShouldEqual(FactEvidence(_partial));
    [Fact] void should_preserve_identical_fact_evidence_when_reversed() => FactEvidence(_reversed).ShouldEqual(FactEvidence(_separateProject));
    [Fact] void should_preserve_identical_fact_evidence_when_relocated() => FactEvidence(_relocated).ShouldEqual(FactEvidence(_separateProject));

    ArtifactFact QueryArtifact() => _separateProject.Facts.OfType<ArtifactFact>().Single(_ => _.Definition.Key.Kind == ArtifactKind.Query);
    ArtifactFact ReadModelArtifact() => _separateProject.Facts.OfType<ArtifactFact>().Single(_ => _.Definition.Key.Kind == ArtifactKind.ReadModel);
    RelationshipFact Returns() => _separateProject.Facts.OfType<RelationshipFact>().Single();
    SpecificationScenarioFact Scenario() => _separateProject.Facts.OfType<SpecificationScenarioFact>().Single();
    SpecificationStepFact Step() => _separateProject.Facts.OfType<SpecificationStepFact>().Single();
    SpecificationValueFact Value(SpecificationValueKey key) => _separateProject.Facts.OfType<SpecificationValueFact>().Single(_ => _.Definition.Key == key);
    SpecificationSyntax Lowered() => _lowering.Application.Modules.SelectMany(_ => _.Features).SelectMany(_ => _.Slices).Single(_ => _.Specifications.Any()).Specifications.Single();
    QuerySyntax LoweredQuery() => _lowering.Application.Modules.SelectMany(_ => _.Features).SelectMany(_ => _.Slices).Single(_ => _.Queries.Any()).Queries.Single();

    static bool CanAnalyze()
    {
        var framework = Source(string.Empty, "Testing/Framework.cs", GeneratedQuerySpecificationSources.Framework);
        var applicationSource = Source(string.Empty, "Projects/Overview/ListProjects/ProjectOverview.cs", GeneratedQuerySpecificationSources.Application);
        var scenario = Source(string.Empty, "Projects/Overview/ListProjects/when_project_by_id_is_queried.cs", GeneratedQuerySpecificationSources.Scenario);
        var application = Analyzed.Project("Projects", [], framework, applicationSource);
        var specifications = Analyzed.Project("Projects.Specifications", [application.ToMetadataReference()], scenario);
        return new ArcSpecificationFactAdapter().CanAnalyze(new DotNetAnalysisContext([
            Project("Projects", DotNetProjectRole.Application, application, string.Empty),
            Project("Projects.Specifications", DotNetProjectRole.Specifications, specifications, string.Empty)
        ]));
    }

    static AdapterContribution Analyze(ProjectShape shape, bool reverseProjects, bool reverseTrees, string root, string? scenarioSource = null)
    {
        var framework = Source(root, "Testing/Framework.cs", GeneratedQuerySpecificationSources.Framework);
        var application = Source(root, "Projects/Overview/ListProjects/ProjectOverview.cs", GeneratedQuerySpecificationSources.Application);
        var scenario = Source(root, "Projects/Overview/ListProjects/when_project_by_id_is_queried.cs", scenarioSource ?? GeneratedQuerySpecificationSources.Scenario);
        var projects = shape switch
        {
            ProjectShape.Same => SameProject([framework, application, scenario], root),
            ProjectShape.Separate => SeparateProjects([framework, application], [scenario], reverseTrees, root),
            ProjectShape.Sibling => SiblingProjects([framework], [application], [scenario], reverseTrees, root),
            _ => []
        };
        if (reverseProjects)
        {
            projects = [.. projects.Reverse()];
        }

        return new ArcSpecificationFactAdapter().Analyze(
            new DotNetAnalysisContext(projects),
            new DotNetAdapterOptions { Module = "Projects", NamespaceSegmentsToSkip = 1 });
    }

    static AdapterContribution AnalyzePartial(bool reverseTrees)
    {
        var framework = Source(string.Empty, "Testing/Framework.cs", GeneratedQuerySpecificationSources.Framework);
        var application = Source(string.Empty, "Projects/Overview/ListProjects/ProjectOverview.cs", GeneratedQuerySpecificationSources.Application);
        (string Path, string Text)[] scenarios =
        [
            Source(string.Empty, "Projects/Overview/ListProjects/when_project_by_id_is_queried.cs", GeneratedQuerySpecificationSources.PartialScenarioFields),
            Source(string.Empty, "Projects/Overview/ListProjects/Z.when_project_by_id_is_queried.cs", GeneratedQuerySpecificationSources.PartialScenarioBehavior)
        ];
        if (reverseTrees)
        {
            scenarios = [.. scenarios.Reverse()];
        }

        return new ArcSpecificationFactAdapter().Analyze(
            new DotNetAnalysisContext(SeparateProjects([framework, application], scenarios, reverseTrees: false, string.Empty)),
            new DotNetAdapterOptions { Module = "Projects", NamespaceSegmentsToSkip = 1 });
    }

    static DotNetProjectCompilation[] SameProject((string Path, string Text)[] sources, string root)
    {
        var compilation = Analyzed.Project("Projects", [], sources);
        return [Project("Projects", DotNetProjectRole.Application, compilation, root)];
    }

    static DotNetProjectCompilation[] SeparateProjects(
        (string Path, string Text)[] applicationSources,
        (string Path, string Text)[] specificationSources,
        bool reverseTrees,
        string root)
    {
        if (reverseTrees)
        {
            applicationSources = [.. applicationSources.Reverse()];
            specificationSources = [.. specificationSources.Reverse()];
        }

        var application = Analyzed.Project("Projects", [], applicationSources);
        var specifications = Analyzed.Project("Projects.Specifications", [application.ToMetadataReference()], specificationSources);
        return
        [
            Project("Projects", DotNetProjectRole.Application, application, root),
            Project("Projects.Specifications", DotNetProjectRole.Specifications, specifications, root)
        ];
    }

    static DotNetProjectCompilation[] SiblingProjects(
        (string Path, string Text)[] frameworkSources,
        (string Path, string Text)[] applicationSources,
        (string Path, string Text)[] specificationSources,
        bool reverseTrees,
        string root)
    {
        if (reverseTrees)
        {
            frameworkSources = [.. frameworkSources.Reverse()];
            applicationSources = [.. applicationSources.Reverse()];
            specificationSources = [.. specificationSources.Reverse()];
        }

        var framework = Analyzed.Project("Projects.Framework", [], frameworkSources);
        var application = Analyzed.Project("Projects", [framework.ToMetadataReference()], applicationSources);
        var specifications = Analyzed.Project("Projects.Specifications", [framework.ToMetadataReference(), application.ToMetadataReference()], specificationSources);
        return
        [
            Project("Projects.Framework", DotNetProjectRole.Application, framework, root),
            Project("Projects", DotNetProjectRole.Application, application, root),
            Project("Projects.Specifications", DotNetProjectRole.Specifications, specifications, root)
        ];
    }

    static DotNetProjectCompilation Project(string name, DotNetProjectRole role, Microsoft.CodeAnalysis.Compilation compilation, string root) =>
        SourceProjects.Create(name, role, compilation, relativePathFor: tree => Relative(tree.FilePath, root));

    static (string Path, string Text) Source(string root, string path, string text) => ($"{root}{path}", text);

    static string Relative(string path, string root) => string.IsNullOrEmpty(root) ? path : path[root.Length..];

    static string Source(IReadOnlyList<AdapterContribution> contributions)
    {
        var source = contributions.FirstOrDefault(_ => _.Facts.OfType<ArtifactFact>().Any());
        return source is null ? SourceRaw(contributions) : SourceRaw([.. contributions, Concepts(source)]);
    }

    static string SourceRaw(IReadOnlyList<AdapterContribution> contributions)
    {
        var graph = new GenerationResolver().Resolve(contributions);
        var lowering = new ScreenplayLowerer().Lower(graph, "Projects");
        return new ScreenplayPrinter().Print(lowering.Application);
    }

    static AdapterContribution Concepts(AdapterContribution contribution)
    {
        var adapter = new AdapterIdentity { Id = "test.query-concepts", Version = "1.0.0" };
        var artifactEvidence = contribution.Facts.OfType<ArtifactFact>().First().Evidence;
        var concepts = contribution.Facts
            .OfType<ArtifactFact>()
            .SelectMany(_ => _.Definition.Properties)
            .Select(_ => _.Type)
            .Where(_ => _.Subject is not null &&
                (string.Equals(_.Name, "ProjectId", StringComparison.Ordinal) ||
                 string.Equals(_.Name, "ProjectName", StringComparison.Ordinal)))
            .GroupBy(_ => _.Subject!)
            .Select(group => (
                Subject: group.Key,
                group.First().Name,
                Primitive: string.Equals(group.First().Name, "ProjectId", StringComparison.Ordinal)
                    ? GenerationPrimitiveKind.Uuid
                    : GenerationPrimitiveKind.Text))
            .OrderBy(_ => _.Subject.Value, StringComparer.Ordinal)
            .SelectMany(concept => new GenerationFact[]
            {
                new ArtifactFact
                {
                    Id = new FactId { Value = $"test.query-concept-artifact:{concept.Subject.Value}" },
                    Subject = concept.Subject,
                    Evidence = new Evidence
                    {
                        Adapter = adapter,
                        Strength = EvidenceStrength.Exact,
                        Source = artifactEvidence.Source
                    },
                    Definition = new ArtifactDefinition
                    {
                        Key = new ArtifactKey { Subject = concept.Subject, Kind = ArtifactKind.Concept },
                        Name = concept.Name
                    }
                },
                new ConceptRepresentationFact
                {
                    Id = new FactId { Value = $"test.query-concept-representation:{concept.Subject.Value}" },
                    Subject = concept.Subject,
                    Evidence = new Evidence
                    {
                        Adapter = adapter,
                        Strength = EvidenceStrength.Exact,
                        Source = artifactEvidence.Source
                    },
                    Definition = new ConceptRepresentationDefinition
                    {
                        Concept = concept.Subject,
                        Kind = ConceptRepresentationKind.Primitive,
                        Primitive = concept.Primitive
                    }
                }
            })
            .ToArray();
        return new AdapterContribution { Adapter = adapter, Facts = concepts };
    }

    static string[] FactEvidence(AdapterContribution contribution) =>
    [
        .. contribution.Facts
            .OrderBy(_ => _.Id.Value, StringComparer.Ordinal)
            .Select(_ => $"{_.Id.Value}|{_.Evidence.Source?.Path}:{_.Evidence.Source?.StartLine}:{_.Evidence.Source?.StartColumn}:{_.Evidence.Source?.EndLine}:{_.Evidence.Source?.EndColumn}")
    ];

    enum ProjectShape
    {
        Same = 0,
        Separate = 1,
        Sibling = 2
    }
}
