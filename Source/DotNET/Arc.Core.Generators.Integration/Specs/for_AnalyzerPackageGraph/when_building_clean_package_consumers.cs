// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Core.Generators.Integration.Specs.Testing;

namespace Cratis.Arc.Core.Generators.Integration.Specs.for_AnalyzerPackageGraph;

/// <summary>
/// Verifies the analyzer package graph through clean consumer restores and builds against a local feed.
/// </summary>
/// <param name="fixture">The shared package-graph fixture.</param>
[Collection(PackageGraphCollection.Name)]
public class when_building_clean_package_consumers(PackageGraphFixture fixture) : Specification
{
    readonly PackageGraphFixture _fixture = fixture;

    /// <summary>
    /// Verifies that every consumer builds with zero warnings and zero errors.
    /// </summary>
    [Fact]
    public void should_build_every_consumer_cleanly()
    {
        foreach (var consumer in _fixture.Consumers)
        {
            consumer.BuildWasClean.ShouldBeTrue();
        }
    }

    /// <summary>
    /// Verifies that every restored integration package is byte-for-byte identical to its local-feed package.
    /// </summary>
    [Fact]
    public void should_restore_every_integration_package_from_the_local_feed()
    {
        foreach (var consumer in _fixture.Consumers)
        {
            consumer.PackagesHaveLocalProvenance.ShouldBeTrue();
        }
    }

    /// <summary>
    /// Verifies exact MSBuild-resolved analyzer, code-fix, and generator counts across the consumer matrix.
    /// </summary>
    [Fact]
    public void should_resolve_every_expected_analyzer_code_fix_and_generator_exactly_once()
    {
        foreach (var consumer in _fixture.Consumers)
        {
            consumer.ArcAnalyzerCount.ShouldEqual(consumer.Definition.ArcAnalyzerCount);
            consumer.ArcCodeFixCount.ShouldEqual(consumer.Definition.ArcCodeFixCount);
            consumer.ArcGeneratorCount.ShouldEqual(consumer.Definition.ArcGeneratorCount);
            consumer.ChronicleAnalyzerCount.ShouldEqual(consumer.Definition.ChronicleAnalyzerCount);
            consumer.ChronicleCodeFixCount.ShouldEqual(consumer.Definition.ChronicleCodeFixCount);
        }
    }

    /// <summary>
    /// Verifies that generated marker and query metadata sources are not duplicated.
    /// </summary>
    [Fact]
    public void should_not_duplicate_generated_marker_or_query_output()
    {
        foreach (var consumer in _fixture.Consumers)
        {
            var expectedGeneratedCount = consumer.Definition.ArcGeneratorCount == 0 ? 0 : 1;
            consumer.GeneratedMarkerCount.ShouldEqual(expectedGeneratedCount);
            consumer.GeneratedQueryMetadataCount.ShouldEqual(expectedGeneratedCount);
        }
    }

    /// <summary>
    /// Verifies that generated query metadata retains the sample read model and query method semantics.
    /// </summary>
    [Fact]
    public void should_generate_metadata_for_the_sample_read_model_and_query()
    {
        foreach (var consumer in _fixture.Consumers)
        {
            var shouldGenerateMetadata = consumer.Definition.ArcGeneratorCount > 0;
            consumer.GeneratedMetadataContainsReadModel.ShouldEqual(shouldGenerateMetadata);
            consumer.GeneratedMetadataContainsQuery.ShouldEqual(shouldGenerateMetadata);
        }
    }

    /// <summary>
    /// Verifies that Workspaces and Composition implementation binaries do not enter consumer output.
    /// </summary>
    [Fact]
    public void should_not_copy_workspaces_or_composition_binaries_to_consumer_output()
    {
        foreach (var consumer in _fixture.Consumers)
        {
            consumer.ForbiddenOutputFiles.ShouldBeEmpty();
        }
    }
}
