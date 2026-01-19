// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an implementation of <see cref="IWebSocketConnectionHandler"/>.
/// </summary>
/// <param name="arcOptions">The <see cref="ArcOptions"/>.</param>
/// <param name="handlerLogger">The <see cref="ILogger"/>.</param>
[Singleton]
public class WebSocketConnectionHandler(IOptions<ArcOptions> arcOptions, ILogger<WebSocketConnectionHandler> handlerLogger) : IWebSocketConnectionHandler
{
    const int BufferSize = 1024 * 4;

    /// <inheritdoc/>
    public async Task HandleIncomingMessages(IWebSocket webSocket, CancellationToken token)
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

                    received = null;
                    received = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    handlerLogger.ReceivedMessage();

                    // Handle ping messages
                    if (received.MessageType == System.Net.WebSockets.WebSocketMessageType.Text && !received.CloseStatus.HasValue)
                    {
                        await HandlePotentialPingMessage(webSocket, buffer, received.Count, token);
                    }
                }
                while (!received.CloseStatus.HasValue);
            }
            catch (TaskCanceledException)
            {
                handlerLogger.CloseConnection(received?.CloseStatusDescription);
                await webSocket.CloseAsync(received?.CloseStatus ?? WebSocketCloseStatus.Empty, received?.CloseStatusDescription, token);
            }
            finally
            {
                if (received is not null)
                {
                    handlerLogger.CloseConnection(received.CloseStatusDescription);
                    await webSocket.CloseAsync(received.CloseStatus ?? WebSocketCloseStatus.Empty, received.CloseStatusDescription, token);
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
        IWebSocket webSocket,
        QueryResult queryResult,
        CancellationToken token)
    {
        try
        {
            var envelope = WebSocketMessage.CreateData(queryResult);
            var message = JsonSerializer.SerializeToUtf8Bytes(envelope, arcOptions.Value.JsonSerializerOptions);
            await webSocket.SendAsync(message, System.Net.WebSockets.WebSocketMessageType.Text, true, token);
            message = null;
            return null;
        }
        catch (Exception ex)
        {
            handlerLogger.ErrorSendingMessage(ex);
            return ex;
        }
    }

    async Task HandlePotentialPingMessage(IWebSocket webSocket, byte[] buffer, int count, CancellationToken token)
    {
        try
        {
            var messageText = System.Text.Encoding.UTF8.GetString(buffer, 0, count);
            var message = JsonSerializer.Deserialize<WebSocketMessage>(messageText, arcOptions.Value.JsonSerializerOptions);

            if (message is not null && message.Type == WebSocketMessageType.Ping)
            {
                handlerLogger.ReceivedPingMessage();
                var pongMessage = WebSocketMessage.Pong(message.Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                var pongBytes = JsonSerializer.SerializeToUtf8Bytes(pongMessage, arcOptions.Value.JsonSerializerOptions);
                await webSocket.SendAsync(pongBytes, System.Net.WebSockets.WebSocketMessageType.Text, true, token);
                handlerLogger.SentPongMessage();
            }
        }
        catch (JsonException)
        {
            // Not a valid JSON message or not a ping message, ignore
        }
        catch (Exception ex)
        {
            handlerLogger.ErrorHandlingPingMessage(ex);
        }
    }
}