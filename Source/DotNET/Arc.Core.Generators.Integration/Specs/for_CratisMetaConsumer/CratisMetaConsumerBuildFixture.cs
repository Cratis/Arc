// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Core.Generators.Integration.Specs.Testing;

namespace Cratis.Arc.Core.Generators.Integration.Specs.for_CratisMetaConsumer;

/// <summary>
/// Executes the Cratis + Cratis.Arc.MongoDB consumer build scenario once for the test class.
/// </summary>
public sealed class CratisMetaConsumerBuildFixture
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CratisMetaConsumerBuildFixture"/> class.
    /// </summary>
    public CratisMetaConsumerBuildFixture() => Result = CratisMetaConsumerBuildScenario.Execute();

    /// <summary>
    /// Gets the result of packing the meta-package closure and building the consumer from the packed nupkgs.
    /// </summary>
    public CratisMetaConsumerBuildResult Result { get; }
}
