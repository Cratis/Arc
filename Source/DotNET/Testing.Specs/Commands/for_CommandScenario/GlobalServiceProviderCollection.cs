// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.for_CommandScenario;

/// <summary>
/// Serializes the specifications that set the process-wide service provider, so they cannot observe each other's host.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GlobalServiceProviderCollection
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "Global service provider";
}
