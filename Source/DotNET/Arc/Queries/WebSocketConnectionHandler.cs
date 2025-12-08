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
        logger ??= handlerLogger;
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
                    logger.ObservableReceivedMessage();
                }
                while (!received.CloseStatus.HasValue);
            }
            catch (TaskCanceledException)
            {
                logger.ObservableCloseConnection(received?.CloseStatusDescription);
                await webSocket.CloseOutputAsync(received?.CloseStatus ?? WebSocketCloseStatus.Empty, received?.CloseStatusDescription, token);
            }
            finally
            {
                if (received is not null)
                {
                    logger.ObservableCloseConnection(received.CloseStatusDescription);
                    await webSocket.CloseOutputAsync(received.CloseStatus ?? WebSocketCloseStatus.Empty, received.CloseStatusDescription, token);
                }
            }
        }
        catch (WebSocketException ex)
        {
            logger.ObservableWebSocketErrorReceivingMessage(ex);
        }
        catch (OperationCanceledException)
        {
            logger.ObservableCloseConnection("Operation was cancelled");
        }
        catch (Exception ex)
        {
            logger.ObservableErrorReceivingMessage(ex);
        }
        finally
        {
            logger.ObservableClientDisconnected();
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
        logger ??= handlerLogger;
        try
        {
            var message = JsonSerializer.SerializeToUtf8Bytes(queryResult, jsonSerializerOptions);
            await webSocket.SendAsync(message, WebSocketMessageType.Text, true, token);
            message = null!;
            return null;
        }
        catch (Exception ex)
        {
            logger.ObservableErrorSendingMessage(ex);
            return ex;
        }
    }
}