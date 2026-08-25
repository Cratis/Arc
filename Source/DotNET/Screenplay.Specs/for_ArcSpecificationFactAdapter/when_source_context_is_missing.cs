// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Generation;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

public class when_source_context_is_missing : Specification
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

        namespace Projects.Projects.Registration.RegisterProject.when_rejecting;

        public class when_rejecting
        {
            readonly CommandScenario<RegisterProject> _scenario = new();
            Result _result = null!;

            async Task Because() => _result = await _scenario.Execute(new RegisterProject(""));

            [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
        }
        """;

    AdapterContribution _contribution = null!;

    void Because()
    {
        var compilation = Analyzed.Project(
            "Projects",
            [],
            ("Source/Projects/Registration/RegisterProject/RegisterProject.cs", Slice),
            ("Source/Projects/Registration/RegisterProject/when_rejecting.cs", Scenario),
            (IntegrationTesting.Path, IntegrationTesting.Source));
        var project = new DotNetProjectCompilation
        {
            Name = "Projects",
            Role = DotNetProjectRole.Application,
            Compilation = compilation,
            AuthoredSyntaxTrees = compilation.SyntaxTrees.ToHashSet()
        };
        _contribution = new ArcSpecificationFactAdapter().Analyze(
            new DotNetAnalysisContext([project]),
            new DotNetAdapterOptions { FeatureRoot = "Source", Module = "Projects", NamespaceSegmentsToSkip = 1 });
    }

    [Fact] void should_report_the_missing_source_context() => _contribution.Diagnostics.Single().Code.ShouldEqual(DotNetSourceStructureDiagnosticCodes.MissingSourceContext);
    [Fact] void should_publish_no_scenario_without_target_context() => _contribution.Facts.OfType<SpecificationScenarioFact>().ShouldBeEmpty();
    [Fact] void should_publish_no_placement_without_target_context() => _contribution.Facts.OfType<ArtifactPlacementFact>().ShouldBeEmpty();
}
