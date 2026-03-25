// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries;

/// <summary>
/// Maps the fixed <see cref="IObservableQueryDemultiplexer"/> endpoints onto the provided <see cref="IEndpointMapper"/>.
/// </summary>
public static class ObservableQueryDemultiplexerEndpointMapper
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
    /// The SSE subscribe POST endpoint route.
    /// </summary>
    public const string SseSubscribeRoute = "/.cratis/queries/sse/subscribe";

    /// <summary>
    /// The SSE unsubscribe POST endpoint route.
    /// </summary>
    public const string SseUnsubscribeRoute = "/.cratis/queries/sse/unsubscribe";

    /// <summary>
    /// Maps the WebSocket, SSE, and SSE subscribe/unsubscribe endpoints for the observable query hub.
    /// </summary>
    /// <param name="mapper">The <see cref="IEndpointMapper"/> to register routes on.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
#pragma warning disable IDE0060 // Remove unused parameter — kept for API consistency with other endpoint mappers
    public static void MapObservableQueryDemultiplexerEndpoints(this IEndpointMapper mapper, IServiceProvider serviceProvider)
#pragma warning restore IDE0060
    {
        MapWebSocketEndpoint(mapper);
        MapSseEndpoint(mapper);
        MapSseSubscribeEndpoint(mapper);
        MapSseUnsubscribeEndpoint(mapper);
    }

    static void MapWebSocketEndpoint(IEndpointMapper mapper)
    {
        const string endpointName = "ObservableQueryDemultiplexerWebSocket";
        if (mapper.EndpointExists(endpointName))
        {
            return;
        }

        var metadata = new EndpointMetadata(
            endpointName,
            "Observable query demultiplexer — WebSocket transport. Clients subscribe to observable queries and receive streaming results over a single persistent connection.",
            ["Cratis Observable Query Demultiplexer"],
            AllowAnonymous: false);

        mapper.MapGet(
            WebSocketRoute,
            async context =>
            {
                var hub = context.RequestServices.GetRequiredService<IObservableQueryDemultiplexer>();
                await hub.HandleWebSocketConnection(context);
            },
            metadata);
    }

    static void MapSseEndpoint(IEndpointMapper mapper)
    {
        const string endpointName = "ObservableQueryDemultiplexerSSE";
        if (mapper.EndpointExists(endpointName))
        {
            return;
        }

        var metadata = new EndpointMetadata(
            endpointName,
            "Observable query demultiplexer — Server-Sent Events transport. Opens a multiplexed SSE stream. The server sends a Connected message with the connection identifier; use the subscribe/unsubscribe POST endpoints to manage subscriptions.",
            ["Cratis Observable Query Demultiplexer"],
            AllowAnonymous: false);

        mapper.MapGet(
            SseRoute,
            async context =>
            {
                var hub = context.RequestServices.GetRequiredService<IObservableQueryDemultiplexer>();
                await hub.HandleSSEConnection(context);
            },
            metadata);
    }

    static void MapSseSubscribeEndpoint(IEndpointMapper mapper)
    {
        const string endpointName = "ObservableQueryDemultiplexerSSESubscribe";
        if (mapper.EndpointExists(endpointName))
        {
            return;
        }

        var metadata = new EndpointMetadata(
            endpointName,
            "Observable query demultiplexer — SSE subscribe. POST a JSON body with connectionId, queryId, and subscription request to start receiving results over the associated SSE stream.",
            ["Cratis Observable Query Demultiplexer"],
            AllowAnonymous: false);

        mapper.MapPost(
            SseSubscribeRoute,
            async context =>
            {
                var hub = context.RequestServices.GetRequiredService<IObservableQueryDemultiplexer>();
                await hub.HandleSSESubscribe(context);
            },
            metadata);
    }

    static void MapSseUnsubscribeEndpoint(IEndpointMapper mapper)
    {
        const string endpointName = "ObservableQueryDemultiplexerSSEUnsubscribe";
        if (mapper.EndpointExists(endpointName))
        {
            return;
        }

        var metadata = new EndpointMetadata(
            endpointName,
            "Observable query demultiplexer — SSE unsubscribe. POST a JSON body with connectionId and queryId to stop receiving results for that subscription.",
            ["Cratis Observable Query Demultiplexer"],
            AllowAnonymous: false);

        mapper.MapPost(
            SseUnsubscribeRoute,
            async context =>
            {
                var hub = context.RequestServices.GetRequiredService<IObservableQueryDemultiplexer>();
                await hub.HandleSSEUnsubscribe(context);
            },
            metadata);
    }
}
