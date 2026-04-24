// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.Discovery;

/// <summary>
/// Defines a service responsible for discovering and mapping command and query endpoints, including extracting XML documentation metadata.
/// </summary>
public interface IDiscoveryService
{
    /// <summary>
    /// Discovers all registered commands and queries, mapping their endpoints and retrieving associated documentation.
    /// </summary>
    /// <returns>A tuple containing lists of discovered commands and queries metadata.</returns>
    (List<CommandDiscoveryMetadata> Commands, List<QueryDiscoveryMetadata> Queries) DiscoverAllEndpoints();
}
