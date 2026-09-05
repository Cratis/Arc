// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Generation;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

public class when_source_folder_and_namespace_placements_conflict : Specification
{
    const string Slice = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Projects.Projects.Registration.RenameProject;

        [EventType]
        public record ProjectRenamed(string Name);

        [Command]
        public record RenameProject(string Name)
        {
            public ProjectRenamed Handle() => new(Name);
        }
        """;

    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Projects.Projects.Registration.RenameProject;
        using Xunit;

        namespace Projects.Projects.Registration.RenameProject.when_rejecting_an_empty_name;

        public class when_rejecting_an_empty_name
        {
            readonly CommandScenario<RenameProject> _scenario = new();
            Result _result = null!;

            async Task Because() => _result = await _scenario.Execute(new RenameProject(""));

            [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
        }
        """;

    AdapterContribution _contribution = null!;

    void Because()
    {
        var compilation = Analyzed.Project(
            "Projects",
            [],
            ("Source/Projects/Registration/RegisterProject/RenameProject.cs", Slice),
            ("Source/Projects/Registration/RenameProject/when_rejecting_an_empty_name.cs", Scenario),
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

    [Fact] void should_report_the_typed_folder_namespace_conflict() => _contribution.Diagnostics.Single().Code.ShouldEqual(DotNetSourceStructureDiagnosticCodes.ConflictingStructure);
    [Fact] void should_retain_the_target_subject() => _contribution.Diagnostics.Single().Subject.ShouldNotBeNull();
    [Fact] void should_contribute_no_target_placement() => _contribution.Facts.OfType<ArtifactPlacementFact>().ShouldBeEmpty();
    [Fact] void should_publish_no_partial_scenario() => _contribution.Facts.OfType<SpecificationScenarioFact>().ShouldBeEmpty();
    [Fact] void should_publish_no_partial_steps() => _contribution.Facts.OfType<SpecificationStepFact>().ShouldBeEmpty();
    [Fact] void should_publish_no_partial_values() => _contribution.Facts.OfType<SpecificationValueFact>().ShouldBeEmpty();
}
