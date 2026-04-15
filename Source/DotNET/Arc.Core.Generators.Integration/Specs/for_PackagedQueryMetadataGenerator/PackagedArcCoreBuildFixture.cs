// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Core.Generators.Integration.Specs.Testing;

namespace Cratis.Arc.Core.Generators.Integration.Specs.for_PackagedQueryMetadataGenerator;

/// <summary>
/// Executes the packaged Arc.Core integration scenario once for the test class.
/// </summary>
public sealed class PackagedArcCoreBuildFixture
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PackagedArcCoreBuildFixture"/> class.
    /// </summary>
    public PackagedArcCoreBuildFixture() => Result = PackagedArcCoreBuildScenario.Execute();

    /// <summary>
    /// Gets the result of packing Arc.Core and building the sample consumer app from the packed nupkg.
    /// </summary>
    public PackagedArcCoreBuildResult Result { get; }
}