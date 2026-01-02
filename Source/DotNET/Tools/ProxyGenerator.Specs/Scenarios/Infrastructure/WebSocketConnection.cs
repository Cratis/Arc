// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using System.Text;

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

#pragma warning disable MA0101 // String contains an implicit end of line character

/// <summary>
/// Represents a WebSocket connection bridged between JavaScript and .NET.
/// </summary>
/// <param name="id">The WebSocket connection ID.</param>
/// <param name="url">The WebSocket URL.</param>
/// <param name="runtime">The JavaScript runtime.</param>
sealed class WebSocketConnection(string id, string url, JavaScriptRuntime runtime) : IDisposable
{
    readonly string _id = id;
    readonly string _url = url;
    readonly JavaScriptRuntime _runtime = runtime;
    readonly CancellationTokenSource _cts = new();
    ClientWebSocket? _webSocket;
    bool _disposed;

    public async Task ConnectAsync()
    {
        try
        {
            _webSocket = new ClientWebSocket();

            // Convert http/https URL to ws/wss
            var uri = new Uri(_url);
            var wsUri = new UriBuilder(uri)
            {
                Scheme = uri.Scheme == "https" ? "wss" : "ws"
            }.Uri;

            // Set the Origin header to match the server URL, as browsers do
            // This is often required by ASP.NET Core WebSocket middleware for security
            var serverUri = new Uri(_runtime.Evaluate<string>("globalThis.Globals.origin") ?? _url);
            var origin = $"{serverUri.Scheme}://{serverUri.Authority}";
            _webSocket.Options.SetRequestHeader("Origin", origin);

            // Connect to the real Kestrel server
            await _webSocket.ConnectAsync(wsUri, _cts.Token);

            // Notify JavaScript that connection is open
            _runtime.Execute($@"
if (globalThis.__webSockets && globalThis.__webSockets['{_id}']) {{
    var ws = globalThis.__webSockets['{_id}'];
    ws.readyState = 1; // OPEN
    if (ws.onopen) ws.onopen({{ type: 'open' }});
}}");

            // Start receiving messages
            _ = Task.Run(ReceiveLoop);
        }
        catch (Exception ex)
        {
            // Notify JavaScript of error
            var errorMsg = ex.Message.Replace("'", "\\'").Replace("\n", "\\n");
            _runtime.Execute($@"
if (globalThis.__webSockets && globalThis.__webSockets['{_id}']) {{
    var ws = globalThis.__webSockets['{_id}'];
    ws.readyState = 3; // CLOSED
    if (ws.onerror) ws.onerror({{ type: 'error', message: '{errorMsg}' }});
    if (ws.onclose) ws.onclose({{ type: 'close', code: 1006, reason: '{errorMsg}' }});
}}");
        }
    }

    public async Task SendAsync(string message)
    {
        if (_webSocket?.State == WebSocketState.Open)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cts.Token);
        }
    }

    async Task ReceiveLoop()
    {
        if (_webSocket is null) return;
        var buffer = new byte[1024 * 4];
        try
        {
            while (_webSocket.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);

                    _runtime.Execute($@"
if (globalThis.__webSockets && globalThis.__webSockets['{_id}']) {{
    var ws = globalThis.__webSockets['{_id}'];
    ws.readyState = 3; // CLOSED
    if (ws.onclose) ws.onclose({{ type: 'close', code: 1000, reason: 'Normal closure' }});
}}");
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                // Ensure we pass a plain JavaScript string as the event data.
                // Escape single quotes, backslashes and newlines so the literal is safe when injected.
                var escaped = message
                    .Replace("\\", "\\\\")
                    .Replace("'", "\\'")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n");

                var injectedSnippet = $@"if (globalThis.__webSockets && globalThis.__webSockets['{_id}']) {{
    var ws = globalThis.__webSockets['{_id}'];
    if (ws.onmessage) ws.onmessage({{ type: 'message', data: '{escaped}' }});
}}";

                _runtime.Execute(injectedSnippet);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception ex)
        {
            var errorMsg = ex.Message.Replace("'", "\\'").Replace("\n", "\\n");
            _runtime.Execute($@"
if (globalThis.__webSockets && globalThis.__webSockets['{_id}']) {{
    var ws = globalThis.__webSockets['{_id}'];
    ws.readyState = 3; // CLOSED
    if (ws.onerror) ws.onerror({{ type: 'error', message: '{errorMsg}' }});
    if (ws.onclose) ws.onclose({{ type: 'close', code: 1006, reason: '{errorMsg}' }});
}}");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cts.Cancel();
            _webSocket?.Dispose();
            _cts.Dispose();
            _disposed = true;
            _runtime.Dispose();
        }
    }
}
