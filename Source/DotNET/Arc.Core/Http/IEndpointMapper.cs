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
    /// Maps an endpoint for the given HTTP method.
    /// </summary>
    /// <param name="httpMethod">The HTTP method (e.g. GET, POST, QUERY) to map.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="handler">The handler for the request.</param>
    /// <param name="metadata">Optional metadata for the endpoint (tags, name, etc.).</param>
    /// <remarks>
    /// This is the general extension point for mapping endpoints — new HTTP methods (such as QUERY)
    /// are supported by passing the method token, without changing the callers. The default
    /// implementation dispatches GET and POST to <see cref="MapGet"/> and <see cref="MapPost"/> and
    /// ignores any other method; implementers that support additional verbs (such as QUERY) override this.
    /// </remarks>
    void MapMethod(string httpMethod, string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null)
    {
        switch (httpMethod.ToUpperInvariant())
        {
            case "GET":
                MapGet(pattern, handler, metadata);
                break;

            case "POST":
                MapPost(pattern, handler, metadata);
                break;
        }
    }

    /// <summary>
    /// Checks if an endpoint with the given name already exists.
    /// </summary>
    /// <param name="name">The endpoint name.</param>
    /// <returns>True if the endpoint exists.</returns>
    bool EndpointExists(string name);
}
