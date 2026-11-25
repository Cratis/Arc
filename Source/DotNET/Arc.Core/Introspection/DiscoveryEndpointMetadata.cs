// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Introspection;

/// <summary>
/// Creates the <see cref="EndpointMetadata"/> of a discovery endpoint, carrying the access the host decided for discovery.
/// </summary>
/// <param name="factory">Creates the metadata for a name, summary, tag and response type.</param>
public class DiscoveryEndpointMetadata(Func<string, string, string, Type, EndpointMetadata> factory)
{
    /// <summary>
    /// Creates the <see cref="EndpointMetadata"/> for an endpoint.
    /// </summary>
    /// <param name="name">The unique name of the endpoint.</param>
    /// <param name="summary">A short description of the endpoint.</param>
    /// <param name="tag">The tag to group the endpoint under.</param>
    /// <param name="responseType">The type of the response.</param>
    /// <returns>The <see cref="EndpointMetadata"/>.</returns>
    public EndpointMetadata For(string name, string summary, string tag, Type responseType) => factory(name, summary, tag, responseType);
}
