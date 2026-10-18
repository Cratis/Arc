// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Core.Generators.Integration.Specs.Testing;

namespace Cratis.Arc.Core.Generators.Integration.Specs.for_AnalyzerPackageGraph;

/// <summary>
/// Exercises a compiled package consumer's generated metadata through the command filter.
/// </summary>
/// <param name="fixture">The shared package-graph fixture.</param>
[Collection(PackageGraphCollection.Name)]
public class when_validating_annotated_concepts(PackageGraphFixture fixture) : Specification
{
    /// <summary>
    /// Verifies that annotated concept properties validate their values in a compiled consumer.
    /// </summary>
    [Fact]
    public void should_validate_the_wrapped_values_without_throwing() => fixture.ConceptAnnotationsValidated.ShouldBeTrue();
}
