// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator;

public static partial class Generator
{
    /// <summary>
    /// Represents the artifacts discovered for a generation run.
    /// </summary>
    /// <param name="Commands">The commands to generate.</param>
    /// <param name="Queries">The queries to generate.</param>
    /// <param name="IdentityDetailsTypes">The identity details types to collect.</param>
    sealed record DiscoveredArtifacts(
        List<CommandDescriptor> Commands,
        List<QueryDescriptor> Queries,
        IEnumerable<Type> IdentityDetailsTypes);
}
