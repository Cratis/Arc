// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Generation;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

public class when_analyzing_an_inherited_partial_read_model_scenario : Specification
{
    const string EventAndCommand = """
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

    const string BaseReadModel = """
        namespace Projects.Projects.Overview.ListProjects;

        public record ProjectOverviewBase
        {
            public string Name { get; init; } = string.Empty;
        }
        """;

    const string FirstReadModelPart = """
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Projections.ModelBound;
        using Projects.Projects.Registration.RegisterProject;

        namespace Projects.Projects.Overview.ListProjects;

        [ReadModel]
        [FromEvent<ProjectRegistered>]
        public partial record ProjectOverview : ProjectOverviewBase
        {
            public int Number { get; init; }
        }
        """;

    const string SecondReadModelPart = """
        namespace Projects.Projects.Overview.ListProjects;

        public enum ProjectStatus
        {
            Active
        }

        public partial record ProjectOverview
        {
            public ProjectStatus Status { get; init; }
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
            readonly ReadModelScenario<ProjectOverview> _scenario = new();

            async Task Establish() => await _scenario.Given
                .ForEventSource(EventSourceId.New())
                .Events(new ProjectRegistered("Screenplay", 42));

            [Fact] void should_project_name() => _scenario.Instance!.Name.ShouldEqual("Screenplay");
            [Fact] void should_project_number() => _scenario.Instance!.Number.ShouldEqual(42);
            [Fact] void should_project_status() => _scenario.Instance!.Status.ShouldEqual(ProjectStatus.Active);
        }
        """;

    AdapterContribution _forward = null!;
    AdapterContribution _reversed = null!;

    void Because()
    {
        _forward = Analyze(reverseSyntaxTrees: false);
        _reversed = Analyze(reverseSyntaxTrees: true);
    }

    [Fact] void should_report_no_diagnostics() => _forward.Diagnostics.ShouldBeEmpty();
    [Fact] void should_recover_inherited_and_partial_properties_in_deterministic_declaration_order() => ReadModelValuePaths(_forward).ShouldEqual(["Name", "Number", "Status"]);
    [Fact] void should_preserve_the_same_declaration_order_when_syntax_trees_are_reversed() => ReadModelValuePaths(_reversed).ShouldEqual(["Name", "Number", "Status"]);
    [Fact] void should_preserve_identical_values_when_syntax_trees_are_reversed() => ReadModelValues(_reversed).ShouldEqual(ReadModelValues(_forward));
    [Fact] void should_preserve_identical_source_evidence_when_syntax_trees_are_reversed() => ReadModelEvidence(_reversed).ShouldEqual(ReadModelEvidence(_forward));
    [Fact] void should_preserve_inherited_and_partial_artifact_properties() => ReadModelArtifact().Definition.Properties.Select(_ => _.Name).ShouldEqual(["Name", "Number", "Status"]);
    [Fact] void should_preserve_exact_read_model_value_types() => ReadModelValueFacts(_forward).Select(_ => _.Definition.Type.Name).ShouldEqual(["String", "Int", "ProjectStatus"]);
    [Fact] void should_preserve_the_enumeration_member_instead_of_its_number() => ReadModelValueFacts(_forward).Single(_ => _.Definition.Key.Path.Single() == "Status").Definition.Scalar.ShouldEqual("Active");

    static AdapterContribution Analyze(bool reverseSyntaxTrees)
    {
        (string Path, string Text)[] sources =
        [
            ("Projects/Registration/RegisterProject/RegisterProject.cs", EventAndCommand),
            ("Projects/Overview/ListProjects/ProjectOverviewBase.cs", BaseReadModel),
            ("Projects/Overview/ListProjects/A.ProjectOverview.cs", FirstReadModelPart),
            ("Projects/Overview/ListProjects/B.ProjectOverview.cs", SecondReadModelPart),
            ("Projects/Overview/ListProjects/when_a_project_was_registered.cs", Scenario),
            (IntegrationTesting.Path, IntegrationTesting.Source)
        ];
        if (reverseSyntaxTrees)
        {
            sources = [.. sources.AsEnumerable().Reverse()];
        }

        var compilation = Analyzed.Project("Projects", [], sources);
        var project = SourceProjects.Create("Projects", DotNetProjectRole.Application, compilation);
        return new ArcSpecificationFactAdapter().Analyze(
            new DotNetAnalysisContext([project]),
            new DotNetAdapterOptions { Module = "Projects", NamespaceSegmentsToSkip = 1 });
    }

    static string[] ReadModelValuePaths(AdapterContribution contribution) =>
    [
        .. contribution.Facts.OfType<SpecificationValueFact>()
            .Where(_ => _.Definition.Key.Step.Index == 1)
            .Select(_ => _.Definition.Key.Path.Single())
    ];

    static string[] ReadModelValues(AdapterContribution contribution) =>
    [
        .. ReadModelValueFacts(contribution)
            .Select(_ => $"{_.Definition.Key.Path.Single()}:{_.Definition.Scalar}")
    ];

    static string[] ReadModelEvidence(AdapterContribution contribution) =>
    [
        .. ReadModelValueFacts(contribution)
            .Select(_ => $"{_.Evidence.Source!.Path}:{_.Evidence.Source.StartLine}:{_.Evidence.Source.StartColumn}:{_.Evidence.Source.EndLine}:{_.Evidence.Source.EndColumn}")
    ];

    ArtifactFact ReadModelArtifact() =>
        _forward.Facts.OfType<ArtifactFact>().Single(_ => _.Definition.Key.Kind == ArtifactKind.ReadModel);

    static SpecificationValueFact[] ReadModelValueFacts(AdapterContribution contribution) =>
    [
        .. contribution.Facts.OfType<SpecificationValueFact>()
            .Where(_ => _.Definition.Key.Step.Index == 1)
    ];
}
