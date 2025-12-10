// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc;

/// <summary>
/// Provides extension methods for mapping HTTP endpoints to an <see cref="ArcApplication"/>.
/// </summary>
public static class EndpointMappingExtensions
{
    /// <summary>
    /// Maps a GET endpoint.
    /// </summary>
    /// <param name="app">The <see cref="ArcApplication"/> to map the endpoint to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="handler">The handler for the request.</param>
    /// <param name="metadata">Optional metadata for the endpoint (tags, name, etc.).</param>
    /// <returns>The <see cref="ArcApplication"/> for chaining.</returns>
    public static ArcApplication MapGet(
        this ArcApplication app,
        string pattern,
        Func<IHttpRequestContext, Task> handler,
        EndpointMetadata? metadata = null)
    {
        app.EndpointMapper?.MapGet(pattern, handler, metadata);
        return app;
    }

    /// <summary>
    /// Maps a POST endpoint.
    /// </summary>
    /// <param name="app">The <see cref="ArcApplication"/> to map the endpoint to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="handler">The handler for the request.</param>
    /// <param name="metadata">Optional metadata for the endpoint (tags, name, etc.).</param>
    /// <returns>The <see cref="ArcApplication"/> for chaining.</returns>
    public static ArcApplication MapPost(
        this ArcApplication app,
        string pattern,
        Func<IHttpRequestContext, Task> handler,
        EndpointMetadata? metadata = null)
    {
        app.EndpointMapper?.MapPost(pattern, handler, metadata);
        return app;
    }
}
