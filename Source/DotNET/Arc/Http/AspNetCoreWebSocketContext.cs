// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.AspNetCore.Http;

/// <summary>
/// Implementation of <see cref="IWebSocketContext"/> for ASP.NET Core.
/// </summary>
/// <param name="httpContext">The <see cref="HttpContext"/>.</param>
public class AspNetCoreWebSocketContext(HttpContext httpContext) : IWebSocketContext
{
    /// <inheritdoc/>
    public bool IsWebSocketRequest => httpContext.WebSockets.IsWebSocketRequest;

    /// <inheritdoc/>
    public async Task<IWebSocket> AcceptWebSocketAsync(CancellationToken cancellationToken = default)
    {
        var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
        return new AspNetCoreWebSocket(webSocket);
    }
}
