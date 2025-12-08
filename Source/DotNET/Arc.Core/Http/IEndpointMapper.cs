// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http;

/// <summary>
/// Defines a system that can map HTTP endpoints for commands and queries.
/// </summary>
public interface IEndpointMapper
{
    /// <summary>
    /// Maps a GET endpoint.
    /// </summary>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="handler">The handler for the request.</param>
    /// <param name="metadata">Optional metadata for the endpoint (tags, name, etc.).</param>
    void MapGet(string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null);

    /// <summary>
    /// Maps a POST endpoint.
    /// </summary>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="handler">The handler for the request.</param>
    /// <param name="metadata">Optional metadata for the endpoint (tags, name, etc.).</param>
    void MapPost(string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null);

    /// <summary>
    /// Checks if an endpoint with the given name already exists.
    /// </summary>
    /// <param name="name">The endpoint name.</param>
    /// <returns>True if the endpoint exists.</returns>
    bool EndpointExists(string name);
}
