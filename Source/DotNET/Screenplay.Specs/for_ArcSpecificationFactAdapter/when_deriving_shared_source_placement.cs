// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Generation;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;
using Cratis.Screenplay.Printing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

public class when_deriving_shared_source_placement : Specification
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

    AdapterContribution _singleProject = null!;
    AdapterContribution _specificationProject = null!;
    AdapterContribution _reversed = null!;
    string _specificationProjectSource = null!;
    string _reversedSource = null!;

    void Because()
    {
        _singleProject = AnalyzeSingleProject();
        _specificationProject = AnalyzeSeparateProjects(false, false);
        _reversed = AnalyzeSeparateProjects(true, true);
        _specificationProjectSource = Source(_specificationProject);
        _reversedSource = Source(_reversed);
    }

    [Fact] void should_derive_the_existing_module() => Placement(_singleProject).Module.ShouldEqual("Projects");
    [Fact] void should_derive_the_existing_feature() => Placement(_singleProject).Features.ShouldContainOnly(["Registration"]);
    [Fact] void should_derive_the_existing_slice() => Placement(_singleProject).Slice.ShouldEqual("RegisterProject");
    [Fact] void should_preserve_application_and_specification_project_placement_parity() => PlacementText(_specificationProject).ShouldEqual(PlacementText(_singleProject));
    [Fact] void should_use_the_application_target_source() => _specificationProject.Facts.OfType<ArtifactPlacementFact>().Single().Evidence.Source!.Path.ShouldEqual("Source/Projects/Registration/RegisterProject/RegisterProject.cs");
    [Fact] void should_be_independent_of_project_and_syntax_tree_order() => _reversedSource.ShouldEqual(_specificationProjectSource);
    [Fact] void should_report_no_diagnostics_for_any_positive_arrangement() => new[] { _singleProject, _specificationProject, _reversed }.SelectMany(_ => _.Diagnostics).ShouldBeEmpty();

    static AdapterContribution AnalyzeSingleProject()
    {
        var compilation = Analyzed.Project(
            "Projects",
            [],
            ("Source/Projects/Registration/RegisterProject/RegisterProject.cs", Slice),
            ("Source/Projects/Registration/RegisterProject/when_rejecting_an_empty_project_name.cs", Scenario),
            (IntegrationTesting.Path, IntegrationTesting.Source));
        return Analyze([SourceProjects.Create("Projects", DotNetProjectRole.Application, compilation)]);
    }

    static AdapterContribution AnalyzeSeparateProjects(bool reverseProjects, bool reverseSyntaxTrees)
    {
        var applicationSources = new[]
        {
            ("Source/Projects/Registration/RegisterProject/RegisterProject.cs", Slice)
        };
        var application = Analyzed.Project("Projects", [], applicationSources);
        var specificationSources = new[]
        {
            ("Source/Projects/Registration/RegisterProject/when_rejecting_an_empty_project_name.cs", Scenario),
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

        return Analyze(projects);
    }

    static AdapterContribution Analyze(IReadOnlyList<DotNetProjectCompilation> projects) =>
        new ArcSpecificationFactAdapter().Analyze(
            new DotNetAnalysisContext(projects),
            new DotNetAdapterOptions
            {
                FeatureRoot = "Source",
                Module = "Projects",
                NamespaceSegmentsToSkip = 1
            });

    static ArtifactPlacement Placement(AdapterContribution contribution) =>
        contribution.Facts.OfType<ArtifactPlacementFact>().Single().Placement;

    static string PlacementText(AdapterContribution contribution)
    {
        var placement = Placement(contribution);
        return $"{placement.Module}:{string.Join('/', placement.Features)}:{placement.Slice}:{placement.SliceKind}";
    }

    static string Source(AdapterContribution contribution)
    {
        var graph = new GenerationResolver().Resolve([contribution]);
        var lowering = new ScreenplayLowerer().Lower(graph, "Projects");
        return new ScreenplayPrinter().Print(lowering.Application);
    }
}
