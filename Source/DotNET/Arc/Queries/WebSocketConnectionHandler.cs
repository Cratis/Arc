// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using System.Text.Json;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an implementation of <see cref="IWebSocketConnectionHandler"/>.
/// </summary>
/// <param name="handlerLogger">The <see cref="ILogger"/>.</param>
[Singleton]
public class WebSocketConnectionHandler(ILogger<WebSocketConnectionHandler> handlerLogger) : IWebSocketConnectionHandler
{
    const int BufferSize = 1024 * 4;

    /// <inheritdoc/>
    public async Task HandleIncomingMessages(WebSocket webSocket, CancellationToken token, ILogger? logger = default)
    {
        try
        {
            var buffer = new byte[BufferSize];
            WebSocketReceiveResult? received = null;
            try
            {
                do
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    received = null!;
                    received = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    handlerLogger.ReceivedMessage();
                }
                while (!received.CloseStatus.HasValue);
            }
            catch (TaskCanceledException)
            {
                handlerLogger.CloseConnection(received?.CloseStatusDescription);
                await webSocket.CloseOutputAsync(received?.CloseStatus ?? WebSocketCloseStatus.Empty, received?.CloseStatusDescription, token);
            }
            finally
            {
                if (received is not null)
                {
                    handlerLogger.CloseConnection(received.CloseStatusDescription);
                    await webSocket.CloseOutputAsync(received.CloseStatus ?? WebSocketCloseStatus.Empty, received.CloseStatusDescription, token);
                }
            }
        }
        catch (WebSocketException ex)
        {
            handlerLogger.WebSocketErrorReceivingMessage(ex);
        }
        catch (OperationCanceledException)
        {
            handlerLogger.OperationCancelled();
        }
        catch (Exception ex)
        {
            handlerLogger.ErrorReceivingMessage(ex);
        }
        finally
        {
            handlerLogger.ClientDisconnected();
        }
    }

    /// <inheritdoc/>
    public async Task<Exception?> SendMessage(
        WebSocket webSocket,
        QueryResult queryResult,
        JsonSerializerOptions jsonSerializerOptions,
        CancellationToken token,
        ILogger? logger = null)
    {
        try
        {
            var message = JsonSerializer.SerializeToUtf8Bytes(queryResult, jsonSerializerOptions);
            await webSocket.SendAsync(message, WebSocketMessageType.Text, true, token);
            message = null!;
            return null;
        }
        catch (Exception ex)
        {
            handlerLogger.ErrorSendingMessage(ex);
            return ex;
        }
    }
}