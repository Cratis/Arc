// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using System.Text.Json;
using Cratis.Arc.AspNetCore.Http;
using Cratis.Arc.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries;

/// <summary>
/// Delivers controller validation failures using the direct observable transport's terminal result format.
/// </summary>
internal static class ControllerQueryValidationRejection
{
    /// <summary>
    /// Writes one validation result and completes the requested streaming transport.
    /// </summary>
    /// <param name="httpContext">The controller's HTTP context.</param>
    /// <param name="result">The query result after validation filtering.</param>
    /// <returns>Whether the validation rejection was delivered over WebSocket or SSE.</returns>
    internal static async Task<bool> TryWrite(HttpContext httpContext, QueryResult result)
    {
        if (result.IsValid || !result.IsAuthorized)
        {
            return false;
        }

        var context = new AspNetCoreHttpRequestContext(httpContext);
        var isSse = context.Headers.TryGetValue("Accept", out var accept) &&
            accept.Contains(HttpRequestContextExtensions.SseContentType, StringComparison.OrdinalIgnoreCase);
        if (!context.WebSockets.IsWebSocketRequest && !isSse)
        {
            return false;
        }

        // Match the model-bound endpoint's terminal validation frame, not a data emission.
        var rejection = new QueryResult
        {
            CorrelationId = result.CorrelationId,
            ValidationResults = result.ValidationResults
        };
        var options = context.RequestServices.GetRequiredService<IOptions<ArcOptions>>().Value;
        var typeInfo = options.JsonSerializerOptions.ResolveTypeInfo<QueryResult>();
        if (context.WebSockets.IsWebSocketRequest)
        {
            using var socket = await context.WebSockets.AcceptWebSocket(context.RequestAborted);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(rejection, typeInfo);
            await socket.Send(bytes, System.Net.WebSockets.WebSocketMessageType.Text, true, context.RequestAborted);
            await socket.Close(WebSocketCloseStatus.NormalClosure, "Query validation failed", context.RequestAborted);
            return true;
        }

        context.SetSseResponseHeaders();
        var json = JsonSerializer.Serialize(rejection, typeInfo);
        await context.Write($"data: {json}\n\n", context.RequestAborted);
        return true;
    }
}
