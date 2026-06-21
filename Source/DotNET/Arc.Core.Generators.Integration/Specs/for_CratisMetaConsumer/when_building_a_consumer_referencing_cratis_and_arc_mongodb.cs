// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Core.Generators.Integration.Specs.for_CratisMetaConsumer;

/// <summary>
/// Verifies that a project referencing both the Cratis meta package and Cratis.Arc.MongoDB builds cleanly, proving
/// the analyzer de-duplication target in Cratis.props collapses the duplicate Arc source generator that would
/// otherwise run twice and fail with CS0101 'Cratis.Arc.Generated.GeneratedMarker'.
/// </summary>
/// <param name="fixture">The fixture that executes the consumer build scenario once for the test class.</param>
public class when_building_a_consumer_referencing_cratis_and_arc_mongodb(CratisMetaConsumerBuildFixture fixture) : IClassFixture<CratisMetaConsumerBuildFixture>
{
    readonly Testing.CratisMetaConsumerBuildResult _buildResult = fixture.Result;

    /// <summary>
    /// Verifies that the consumer referencing both Cratis and Cratis.Arc.MongoDB builds successfully.
    /// </summary>
    [Fact]
    public void should_build_successfully() => _buildResult.BuildSucceeded.ShouldBeTrue();

    /// <summary>
    /// Verifies that the consumer build does not fail with a duplicate generated type error.
    /// </summary>
    [Fact]
    public void should_not_emit_a_duplicate_definition_error() => _buildResult.BuildOutput.ShouldNotContain("CS0101");
}
