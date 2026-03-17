// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries;

/// <summary>
/// Maps the fixed <see cref="IObservableQueryHub"/> endpoints onto the provided <see cref="IEndpointMapper"/>.
/// </summary>
public static class ObservableQueryHubEndpointMapper
{
    /// <summary>
    /// The WebSocket endpoint route.
    /// </summary>
    public const string WebSocketRoute = "/.cratis/queries/ws";

    /// <summary>
    /// The Server-Sent Events endpoint route.
    /// </summary>
    public const string SseRoute = "/.cratis/queries/sse";

    /// <summary>
    /// Maps both the WebSocket (<c>/.cratis/queries/ws</c>) and SSE (<c>/.cratis/queries/sse</c>)
    /// endpoints for the observable query hub.
    /// </summary>
    /// <param name="mapper">The <see cref="IEndpointMapper"/> to register routes on.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
#pragma warning disable IDE0060 // Remove unused parameter — kept for API consistency with other endpoint mappers
    public static void MapObservableQueryHubEndpoints(this IEndpointMapper mapper, IServiceProvider serviceProvider)
#pragma warning restore IDE0060
    {
        MapWebSocketEndpoint(mapper);
        MapSseEndpoint(mapper);
    }

    static void MapWebSocketEndpoint(IEndpointMapper mapper)
    {
        const string endpointName = "ObservableQueryHubWebSocket";
        if (mapper.EndpointExists(endpointName))
        {
            return;
        }

        var metadata = new EndpointMetadata(
            endpointName,
            "Observable query hub — WebSocket transport. Clients subscribe to observable queries and receive streaming results over a single persistent connection.",
            ["Cratis Observable Query Hub"],
            AllowAnonymous: false);

        mapper.MapGet(
            WebSocketRoute,
            async context =>
            {
                var hub = context.RequestServices.GetRequiredService<IObservableQueryHub>();
                await hub.HandleWebSocketConnection(context);
            },
            metadata);
    }

    static void MapSseEndpoint(IEndpointMapper mapper)
    {
        const string endpointName = "ObservableQueryHubSSE";
        if (mapper.EndpointExists(endpointName))
        {
            return;
        }

        var metadata = new EndpointMetadata(
            endpointName,
            "Observable query hub — Server-Sent Events transport. Supply the fully qualified query name in the 'query' query-string parameter. Additional query-string parameters are forwarded as query arguments.",
            ["Cratis Observable Query Hub"],
            AllowAnonymous: false);

        mapper.MapGet(
            SseRoute,
            async context =>
            {
                var hub = context.RequestServices.GetRequiredService<IObservableQueryHub>();
                await hub.HandleSSEConnection(context);
            },
            metadata);
    }
}
