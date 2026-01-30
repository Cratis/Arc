// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Cratis.Arc.Queries;
using WebSocketMessageType = System.Net.WebSockets.WebSocketMessageType;

namespace Cratis.Arc.EntityFrameworkCore.Integration;

#pragma warning disable CA1849 // Call async methods when in an async method
#pragma warning disable MA0042 // Use async methods
#pragma warning disable CA1869 // Cache and reuse JsonSerializerOptions

/// <summary>
/// Test WebSocket client for receiving query results from observable queries.
/// </summary>
public class TestWebSocketClient : IDisposable
{
    readonly ClientWebSocket _webSocket;
    readonly CancellationTokenSource _cancellationTokenSource;
    readonly List<QueryResult> _receivedResults = [];
    readonly SemaphoreSlim _receiveLock = new(1, 1);
    Task? _receiveTask;
    bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestWebSocketClient"/> class.
    /// </summary>
    public TestWebSocketClient()
    {
        _webSocket = new ClientWebSocket();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    /// Gets all received query results.
    /// </summary>
    public IReadOnlyList<QueryResult> ReceivedResults
    {
        get
        {
            _receiveLock.Wait();
            try
            {
                return [.. _receivedResults];
            }
            finally
            {
                _receiveLock.Release();
            }
        }
    }

    /// <summary>
    /// Gets the latest received query result.
    /// </summary>
    public QueryResult? LatestResult
    {
        get
        {
            _receiveLock.Wait();
            try
            {
                return _receivedResults.Count > 0 ? _receivedResults[^1] : null;
            }
            finally
            {
                _receiveLock.Release();
            }
        }
    }

    /// <summary>
    /// Gets the number of received messages.
    /// </summary>
    public int MessageCount
    {
        get
        {
            _receiveLock.Wait();
            try
            {
                return _receivedResults.Count;
            }
            finally
            {
                _receiveLock.Release();
            }
        }
    }

    /// <summary>
    /// Connects to the WebSocket endpoint.
    /// </summary>
    /// <param name="uri">The WebSocket URI.</param>
    /// <param name="timeout">Optional connection timeout.</param>
    /// <returns>A task representing the connection operation.</returns>
    public async Task Connect(Uri uri, TimeSpan? timeout = null)
    {
        using var connectionCts = timeout.HasValue
            ? new CancellationTokenSource(timeout.Value)
            : new CancellationTokenSource(TimeSpan.FromSeconds(10));

        await _webSocket.ConnectAsync(uri, connectionCts.Token);
        _receiveTask = ReceiveLoop(_cancellationTokenSource.Token);
    }

    /// <summary>
    /// Waits for a specified number of messages to be received.
    /// </summary>
    /// <param name="count">The number of messages to wait for.</param>
    /// <param name="timeout">The timeout to wait.</param>
    /// <returns>True if the expected number of messages was received, false otherwise.</returns>
    public async Task<bool> WaitForMessages(int count, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        while (!cts.Token.IsCancellationRequested)
        {
            if (MessageCount >= count)
            {
                return true;
            }
            await Task.Delay(50, cts.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
        return MessageCount >= count;
    }

    /// <summary>
    /// Waits for a message matching the predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match.</param>
    /// <param name="timeout">The timeout to wait.</param>
    /// <returns>True if a matching message was received, false otherwise.</returns>
    public async Task<bool> WaitForMessage(Func<QueryResult, bool> predicate, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        while (!cts.Token.IsCancellationRequested)
        {
            _receiveLock.Wait();
            try
            {
                if (_receivedResults.Any(predicate))
                {
                    return true;
                }
            }
            finally
            {
                _receiveLock.Release();
            }
            await Task.Delay(50, cts.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
        return false;
    }

    /// <summary>
    /// Closes the WebSocket connection.
    /// </summary>
    /// <returns>A task representing the close operation.</returns>
    public async Task Close()
    {
        if (_webSocket.State == WebSocketState.Open)
        {
            using var closeCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", closeCts.Token);
        }
        await _cancellationTokenSource.CancelAsync();
        if (_receiveTask is not null)
        {
            await _receiveTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cancellationTokenSource.Cancel();
        _webSocket.Dispose();
        _cancellationTokenSource.Dispose();
        _receiveLock.Dispose();
    }

    async Task ReceiveLoop(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        var messageBuffer = new List<byte>();

        while (!cancellationToken.IsCancellationRequested && _webSocket.State == WebSocketState.Open)
        {
            try
            {
                var result = await _webSocket.ReceiveAsync(buffer, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                messageBuffer.AddRange(buffer.Take(result.Count));

                if (result.EndOfMessage)
                {
                    var json = Encoding.UTF8.GetString(messageBuffer.ToArray());
                    messageBuffer.Clear();

                    try
                    {
                        var message = JsonSerializer.Deserialize<WebSocketDataMessage>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        // Type 0 = Data (the enum is serialized as integer by Cratis.Json EnumConverterFactory)
                        if (message?.Type == 0 && message.Data is not null)
                        {
                            // Parse the nested data structure using JsonElement
                            var dataElement = message.Data.Value;
                            var simpleResult = dataElement.Deserialize<SimpleQueryResult>(new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (simpleResult is not null)
                            {
                                // Convert to QueryResult with the data
                                var queryResult = new QueryResult
                                {
                                    IsAuthorized = simpleResult.IsAuthorized,
                                    Data = simpleResult.Data ?? default!
                                };

                                await _receiveLock.WaitAsync(cancellationToken);
                                try
                                {
                                    _receivedResults.Add(queryResult);
                                }
                                finally
                                {
                                    _receiveLock.Release();
                                }
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // Ignore JSON parsing errors in test client
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (WebSocketException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Internal message structure for WebSocket data messages.
    /// </summary>
    class WebSocketDataMessage
    {
        public int? Type { get; set; }
        public JsonElement? Data { get; set; }
    }

    /// <summary>
    /// Simplified query result for testing without concept converters.
    /// </summary>
    class SimpleQueryResult
    {
        public bool IsSuccess { get; set; }
        public bool IsAuthorized { get; set; }
        public bool HasExceptions { get; set; }
        public JsonElement? Data { get; set; }
    }
}
