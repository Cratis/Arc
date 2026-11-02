// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Http;

/// <summary>
/// Extension methods applying the trusted origin policy to WebSocket connection requests.
/// </summary>
public static class WebSocketOriginGate
{
    /// <summary>
    /// Checks the origin of a WebSocket connection request and answers 403 when it is not trusted.
    /// </summary>
    /// <param name="context">The <see cref="IHttpRequestContext"/> of the request.</param>
    /// <returns>True when the request was refused and must not be processed further; false otherwise.</returns>
    /// <remarks>
    /// Requests that are not WebSocket connection requests are never refused. Trust is decided by
    /// <see cref="Queries.QueryOptions.AllowedWebSocketOrigins"/> and by the scheme, host and port of the request.
    /// </remarks>
    public static bool RejectsUntrustedWebSocketOrigin(this IHttpRequestContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            return false;
        }

        var allowed = context.RequestServices.GetService<IOptions<ArcOptions>>()?.Value.Query.AllowedWebSocketOrigins ?? [];
        var headers = context.Headers;
        var origin = headers.TryGetValue("Origin", out var originValue) ? originValue : null;
        var host = headers.TryGetValue("Host", out var hostValue) ? hostValue : null;

        if (WebSocketOrigins.IsTrusted(origin, host, context.IsHttps, allowed))
        {
            return false;
        }

        var logged = WebSocketOrigins.TryNormalize(origin, out var normalized) ? normalized : "(invalid)";
        context.RequestServices.GetService<ILoggerFactory>()?
            .CreateLogger(typeof(WebSocketOriginGate).FullName!)
            .RefusedWebSocketOrigin(context.Path, logged);
        context.SetStatusCode(HttpStatusCode.Forbidden);
        return true;
    }
}
