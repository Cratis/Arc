// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Generation;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

/// <summary>
/// A given event in a scenario compiled with references of its own is a different symbol from the event the
/// application's command produces, yet it is the same declared type and must still be placed.
/// </summary>
public class when_placing_a_given_event_seen_through_references_of_its_own : Specification
{
    const string Application = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections.ModelBound;

        namespace Projects.Projects.Registration.RegisterProject
        {
            [EventType]
            public record ProjectRegistered(string Name);

            [Command]
            public record RegisterProject(string Name)
            {
                public ProjectRegistered Handle() => new(Name);
            }
        }

        namespace Projects.Projects.Overview.ListProjects
        {
            [ReadModel]
            [FromEvent<global::Projects.Projects.Registration.RegisterProject.ProjectRegistered>]
            public record ProjectOverview
            {
                public string Name { get; init; } = string.Empty;
            }
        }
        """;

    const string OtherNamespaceApplication = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Projects.Projects.Other
        {
            [EventType]
            public record ProjectRegistered(string Name);

            [Command]
            public record RegisterOther(string Name)
            {
                public ProjectRegistered Handle() => new(Name);
            }
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
                .Events(new ProjectRegistered("Screenplay"));

            [Fact] void should_project_name() => _scenario.Instance!.Name.ShouldEqual("Screenplay");
        }
        """;

    Compilation _application;
    Compilation _consumer;
    AdapterContribution _contribution = null!;
    AdapterContribution _control = null!;

    void Because()
    {
        _application = Analyzed.Project("Projects", [], ("Projects/Application.cs", Application));
        _consumer = Consumer(_application);
        _contribution = Analyze(_application, _consumer);

        // The only command producing a ProjectRegistered is in another namespace: the given event is not produced.
        var controlApplication = Analyzed.Project(
            "Projects",
            [],
            ("Projects/Application.cs", Application.Replace("public ProjectRegistered Handle() => new(Name);", "public global::Projects.Projects.Other.ProjectRegistered Handle() => new(Name);", StringComparison.Ordinal)),
            ("Projects/Other.cs", OtherNamespaceApplication));
        _control = Analyze(controlApplication, Consumer(controlApplication));
    }

    static Compilation Consumer(Compilation application) => Analyzed.Project(
        "Projects.Specifications",
        [],
        ("Projects/Overview/ListProjects/when_a_project_was_registered.cs", Scenario),
        (IntegrationTesting.Path, IntegrationTesting.Source))
        .WithReferences(((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Select(_ => MetadataReference.CreateFromFile(_))
            .Append<MetadataReference>(application.ToMetadataReference()));

    static AdapterContribution Analyze(Compilation application, Compilation consumer) => new ArcSpecificationFactAdapter().Analyze(
        new DotNetAnalysisContext(
        [
            SourceProjects.Create("Projects", DotNetProjectRole.Application, application),
            SourceProjects.Create("Projects.Specifications", DotNetProjectRole.Specifications, consumer)
        ]),
        new DotNetAdapterOptions { Module = "Projects", NamespaceSegmentsToSkip = 1 });

    [Fact] void should_compile_the_application() => Analyzed.ErrorsIn(_application).ShouldBeEmpty();
    [Fact] void should_compile_the_consumer() => Analyzed.ErrorsIn(_consumer).ShouldBeEmpty();
    [Fact] void should_compare_distinct_event_types() => SymbolEqualityComparer.Default.Equals(
        _application.GetTypeByMetadataName("Projects.Projects.Registration.RegisterProject.ProjectRegistered"),
        _consumer.GetTypeByMetadataName("Projects.Projects.Registration.RegisterProject.ProjectRegistered")).ShouldBeFalse();
    [Fact] void should_have_no_adapter_diagnostics() => _contribution.Diagnostics.ShouldBeEmpty();
    [Fact] void should_contribute_one_scenario() => _contribution.Facts.OfType<SpecificationScenarioFact>().Count().ShouldEqual(1);
    [Fact] void should_place_the_event_and_read_model_in_their_exact_slices() => _contribution.Facts.OfType<ArtifactPlacementFact>().Select(_ => (_.Placement.Slice, _.Placement.SliceKind)).ShouldContainOnly([("RegisterProject", GenerationSliceKind.StateChange), ("ListProjects", GenerationSliceKind.StateView)]);
    [Fact] void should_not_place_an_event_another_namespace_produces() => _control.Diagnostics.Select(_ => _.Code).ShouldContainOnly("ARCSP0001");
    [Fact] void should_contribute_no_facts_for_an_event_another_namespace_produces() => _control.Facts.ShouldBeEmpty();
}
