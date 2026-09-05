// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Generation;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

public class when_one_scenario_has_invalid_placement : Specification
{
    const string ValidSlice = """
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

    const string InvalidSlice = """
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

    const string ValidScenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Projects.Projects.Registration.RegisterProject;
        using Xunit;

        namespace Projects.Projects.Registration.RegisterProject.when_rejecting_registration;

        public class when_rejecting_registration
        {
            readonly CommandScenario<RegisterProject> _scenario = new();
            Result _result = null!;

            async Task Because() => _result = await _scenario.Execute(new RegisterProject(""));

            [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
        }
        """;

    const string InvalidScenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Projects.Projects.Registration.RenameProject;
        using Xunit;

        namespace Projects.Projects.Registration.RenameProject.when_rejecting_rename;

        public class when_rejecting_rename
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
            ("Source/Projects/Registration/RegisterProject/RegisterProject.cs", ValidSlice),
            ("Source/Projects/Registration/ArchiveProject/RenameProject.cs", InvalidSlice),
            ("Source/Projects/Registration/RegisterProject/when_rejecting_registration.cs", ValidScenario),
            ("Source/Projects/Registration/RenameProject/when_rejecting_rename.cs", InvalidScenario),
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

    [Fact] void should_report_only_the_invalid_target_placement() => _contribution.Diagnostics.Single().Code.ShouldEqual(DotNetSourceStructureDiagnosticCodes.ConflictingStructure);
    [Fact] void should_keep_the_independently_valid_scenario() => _contribution.Facts.OfType<SpecificationScenarioFact>().Single().Definition.Name.ShouldEqual("when_rejecting_registration_when_rejecting_registration");
    [Fact] void should_publish_only_the_valid_scenario_steps() => _contribution.Facts.OfType<SpecificationStepFact>().Count().ShouldEqual(2);
    [Fact] void should_publish_only_the_valid_scenario_value() => _contribution.Facts.OfType<SpecificationValueFact>().Count().ShouldEqual(1);
    [Fact] void should_publish_only_the_valid_target_placement() => _contribution.Facts.OfType<ArtifactPlacementFact>().Single().Placement.Slice.ShouldEqual("RegisterProject");
    [Fact] void should_not_publish_the_blocked_target_artifact() => _contribution.Facts.OfType<ArtifactFact>().Any(_ => _.Definition.Name == "RenameProject").ShouldBeFalse();
}
