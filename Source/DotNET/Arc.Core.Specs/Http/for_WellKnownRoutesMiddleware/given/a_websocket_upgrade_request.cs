// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;

namespace Cratis.Arc.Http.for_WellKnownRoutesMiddleware.given;

public class a_websocket_upgrade_request : a_well_known_routes_middleware
{
    protected bool _handlerCalled;
    protected bool _nextCalled;
    protected bool _result;
    protected bool _accepted;
    protected WebSocketException? _connectError;

    void Establish()
    {
        _middleware.RegisterRoute("GET", "/ws", async ctx =>
        {
            _handlerCalled = true;
            await ctx.WebSockets.AcceptWebSocket();
        });

        StartListener();
    }

    protected async Task Upgrade(string? origin)
    {
        using var client = new ClientWebSocket();
        if (origin is not null)
        {
            client.Options.SetRequestHeader("Origin", origin);
        }

        var connecting = client.ConnectAsync(new Uri($"ws://localhost:{_port}/ws"), CancellationToken.None);
        var context = await _listener.GetContextAsync();
        _result = await _middleware.InvokeAsync(context, _ =>
        {
            _nextCalled = true;
            return Task.FromResult(false);
        });

        if (context.Response.StatusCode == 403)
        {
            context.Response.Close();
        }

        try
        {
            await connecting;
            _accepted = true;
        }
        catch (WebSocketException error)
        {
            _connectError = error;
        }
    }
}
