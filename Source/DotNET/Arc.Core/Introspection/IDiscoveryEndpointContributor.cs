// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Introspection;

/// <summary>
/// Defines a contributor of developer tooling endpoints under <c>/.cratis</c>, mapped together with the introspection endpoints
/// so that they follow the same exposure rules as the catalogs.
/// </summary>
public interface IDiscoveryEndpointContributor
{
    /// <summary>
    /// Maps the endpoints of the contributor.
    /// </summary>
    /// <param name="mapper">The <see cref="IEndpointMapper"/> to map with.</param>
    /// <param name="metadata">The <see cref="DiscoveryEndpointMetadata"/> that describes the access the endpoints get.</param>
    void Map(IEndpointMapper mapper, DiscoveryEndpointMetadata metadata);
}
