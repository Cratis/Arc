// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Generation;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

/// <summary>
/// The given event is held by a base context another project declares and the appended event is named in the
/// scenario, so one event is reached through two compilations and must still be one artifact.
/// </summary>
public class when_one_artifact_is_reached_through_a_base_context_and_its_scenario : Specification
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

    const string Context = """
        using Cratis.Specifications;
        using Projects.Projects.Registration.RegisterProject;

        namespace Projects.Projects.Registration.RegisterProject;

        public class a_registered_project : Specification
        {
            protected readonly ProjectRegistered _registered = new("Screenplay", 42);
        }
        """;

    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Arc.Testing.Commands;
        using Projects.Projects.Registration.RegisterProject;
        using Xunit;

        namespace Projects.Projects.Registration.RegisterProject.when_registering;

        public class when_registering : a_registered_project
        {
            readonly CommandScenario<RegisterProject> _scenario = new();
            CommandResult _result = null!;

            void Establish() => _scenario.Given.ForEventSource("project").Events(_registered);

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

    Compilation _application;
    Compilation _contexts;
    Compilation _consumer;
    AdapterContribution _contribution = null!;

    void Because()
    {
        _application = Analyzed.Project("Projects", [], ("Projects/Registration/RegisterProject/RegisterProject.cs", Slice));
        _contexts = Analyzed.Project(
            "Projects.Specifications.Contexts",
            [_application.ToMetadataReference()],
            ("Contexts/a_registered_project.cs", Context));
        _consumer = Analyzed.Project(
            "Projects.Specifications",
            [],
            ("Projects/Registration/RegisterProject/when_registering.cs", Scenario),
            ("Integration/Assertions.cs", Assertions),
            (IntegrationTesting.Path, IntegrationTesting.Source))
            .WithReferences(((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator).Select(_ => MetadataReference.CreateFromFile(_))
                .Append<MetadataReference>(_application.ToMetadataReference())
                .Append<MetadataReference>(_contexts.ToMetadataReference()));
        _contribution = new ArcSpecificationFactAdapter().Analyze(
            new DotNetAnalysisContext(
            [
                SourceProjects.Create("Projects", DotNetProjectRole.Application, _application),
                SourceProjects.Create("Projects.Specifications.Contexts", DotNetProjectRole.Specifications, _contexts),
                SourceProjects.Create("Projects.Specifications", DotNetProjectRole.Specifications, _consumer)
            ]),
            new DotNetAdapterOptions { Module = "Projects", NamespaceSegmentsToSkip = 1 });
    }

    [Fact] void should_compile_the_application() => Analyzed.ErrorsIn(_application).ShouldBeEmpty();
    [Fact] void should_compile_the_contexts() => Analyzed.ErrorsIn(_contexts).ShouldBeEmpty();
    [Fact] void should_compile_the_consumer() => Analyzed.ErrorsIn(_consumer).ShouldBeEmpty();
    [Fact] void should_compare_distinct_event_types() => SymbolEqualityComparer.Default.Equals(
        _contexts.GetTypeByMetadataName("Projects.Projects.Registration.RegisterProject.ProjectRegistered"),
        _consumer.GetTypeByMetadataName("Projects.Projects.Registration.RegisterProject.ProjectRegistered")).ShouldBeFalse();
    [Fact] void should_have_no_adapter_diagnostics() => _contribution.Diagnostics.ShouldBeEmpty();
    [Fact] void should_contribute_the_given_when_and_then_steps() => _contribution.Facts.OfType<SpecificationStepFact>().Count().ShouldEqual(3);
    [Fact] void should_contribute_one_event_artifact() => _contribution.Facts.OfType<ArtifactFact>().Count(_ => _.Definition.Key.Kind == ArtifactKind.Event).ShouldEqual(1);
}
