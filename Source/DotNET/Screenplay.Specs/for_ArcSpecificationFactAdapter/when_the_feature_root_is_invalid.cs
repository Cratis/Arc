// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Generation;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Arc.Screenplay.for_ArcSpecificationFactAdapter;

public class when_the_feature_root_is_invalid : Specification
{
    AdapterContribution _rooted = null!;
    AdapterContribution _traversing = null!;

    void Because()
    {
        var compilation = Analyzed.Project(
            "Projects",
            [],
            ("Source/Projects/Registration/RegisterProject/RegisterProject.cs", PlacementScenarioSources.Slice),
            ("Source/Projects/Registration/RegisterProject/when_rejecting.cs", PlacementScenarioSources.Scenario),
            (IntegrationTesting.Path, IntegrationTesting.Source));
        var project = SourceProjects.Create("Projects", DotNetProjectRole.Application, compilation);
        _rooted = Analyze(project, "/Source");
        _traversing = Analyze(project, "../Source");
    }

    [Fact] void should_reject_a_rooted_feature_root() => _rooted.Diagnostics.Single().Code.ShouldEqual(DotNetSourceStructureDiagnosticCodes.InvalidPath);
    [Fact] void should_reject_a_traversing_feature_root() => _traversing.Diagnostics.Single().Code.ShouldEqual(DotNetSourceStructureDiagnosticCodes.InvalidPath);
    [Fact] void should_publish_no_scenario_for_either_invalid_root() => new[] { _rooted, _traversing }.All(_ => !_.Facts.OfType<SpecificationScenarioFact>().Any()).ShouldBeTrue();
    [Fact] void should_publish_no_placement_for_either_invalid_root() => new[] { _rooted, _traversing }.All(_ => !_.Facts.OfType<ArtifactPlacementFact>().Any()).ShouldBeTrue();

    static AdapterContribution Analyze(DotNetProjectCompilation project, string featureRoot) =>
        new ArcSpecificationFactAdapter().Analyze(
            new DotNetAnalysisContext([project]),
            new DotNetAdapterOptions
            {
                FeatureRoot = featureRoot,
                Module = "Projects",
                NamespaceSegmentsToSkip = 1
            });
}
