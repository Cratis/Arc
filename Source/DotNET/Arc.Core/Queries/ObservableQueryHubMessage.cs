// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents a protocol message exchanged over the <see cref="ObservableQueryDemultiplexer"/> WebSocket or SSE connection.
/// </summary>
public class ObservableQueryHubMessage
{
    /// <summary>
    /// Gets or sets the type of message.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<ObservableQueryHubMessageType>))]
    public ObservableQueryHubMessageType Type { get; set; }

    /// <summary>
    /// Gets or sets the client-assigned query identifier used to correlate subscriptions with their result updates.
    /// </summary>
    public string? QueryId { get; set; }

    /// <summary>
    /// Gets or sets the optional subscription generation used to reject messages from replaced subscriptions.
    /// </summary>
    public string? SubscriptionGeneration { get; set; }

    /// <summary>
    /// Gets or sets the message payload. Interpretation depends on <see cref="Type"/>:
    /// <list type="bullet">
    ///   <item><description><see cref="ObservableQueryHubMessageType.Subscribe"/> — an <see cref="ObservableQuerySubscriptionRequest"/>.</description></item>
    ///   <item><description><see cref="ObservableQueryHubMessageType.QueryResult"/> — a <see cref="QueryResult"/>.</description></item>
    ///   <item><description><see cref="ObservableQueryHubMessageType.Error"/> — a plain error string.</description></item>
    /// </list>
    /// </summary>
    public object? Payload { get; set; }

    /// <summary>
    /// Gets or sets the Unix millisecond timestamp, used for ping/pong latency tracking.
    /// </summary>
    public long? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the server's keep-alive interval in milliseconds, or <c>0</c> when keep-alive is disabled.
    /// </summary>
    /// <remarks>
    /// Only set on <see cref="ObservableQueryHubMessageType.Connected"/> messages. Clients derive their
    /// idle threshold from this rather than assuming the default interval, so that reconfiguring the server
    /// cannot leave a client declaring healthy connections dead.
    /// </remarks>
    public long? KeepAliveIntervalMs { get; set; }

    /// <summary>
    /// Creates a <see cref="ObservableQueryHubMessageType.QueryResult"/> message.
    /// </summary>
    /// <param name="queryId">The query identifier.</param>
    /// <param name="result">The query result payload.</param>
    /// <returns>A populated <see cref="ObservableQueryHubMessage"/>.</returns>
    public static ObservableQueryHubMessage CreateQueryResult(string queryId, QueryResult result) =>
        CreateQueryResult(queryId, result, null);

    /// <summary>
    /// Creates a generation-aware <see cref="ObservableQueryHubMessageType.QueryResult"/> message.
    /// </summary>
    /// <param name="queryId">The query identifier.</param>
    /// <param name="result">The query result payload.</param>
    /// <param name="subscriptionGeneration">The optional subscription generation.</param>
    /// <returns>A populated <see cref="ObservableQueryHubMessage"/>.</returns>
    public static ObservableQueryHubMessage CreateQueryResult(string queryId, QueryResult result, string? subscriptionGeneration) =>
        new() { Type = ObservableQueryHubMessageType.QueryResult, QueryId = queryId, Payload = result, SubscriptionGeneration = subscriptionGeneration };

    /// <summary>
    /// Creates an <see cref="ObservableQueryHubMessageType.Unauthorized"/> message.
    /// </summary>
    /// <param name="queryId">The query identifier.</param>
    /// <returns>A populated <see cref="ObservableQueryHubMessage"/>.</returns>
    public static ObservableQueryHubMessage CreateUnauthorized(string queryId) =>
        CreateUnauthorized(queryId, null);

    /// <summary>
    /// Creates a generation-aware <see cref="ObservableQueryHubMessageType.Unauthorized"/> message.
    /// </summary>
    /// <param name="queryId">The query identifier.</param>
    /// <param name="subscriptionGeneration">The optional subscription generation.</param>
    /// <returns>A populated <see cref="ObservableQueryHubMessage"/>.</returns>
    public static ObservableQueryHubMessage CreateUnauthorized(string queryId, string? subscriptionGeneration) =>
        new() { Type = ObservableQueryHubMessageType.Unauthorized, QueryId = queryId, SubscriptionGeneration = subscriptionGeneration };

    /// <summary>
    /// Creates an <see cref="ObservableQueryHubMessageType.Error"/> message.
    /// </summary>
    /// <param name="queryId">The query identifier.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A populated <see cref="ObservableQueryHubMessage"/>.</returns>
    public static ObservableQueryHubMessage CreateError(string queryId, string errorMessage) =>
        CreateError(queryId, errorMessage, null);

    /// <summary>
    /// Creates a generation-aware <see cref="ObservableQueryHubMessageType.Error"/> message.
    /// </summary>
    /// <param name="queryId">The query identifier.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="subscriptionGeneration">The optional subscription generation.</param>
    /// <returns>A populated <see cref="ObservableQueryHubMessage"/>.</returns>
    public static ObservableQueryHubMessage CreateError(string queryId, string errorMessage, string? subscriptionGeneration) =>
        new() { Type = ObservableQueryHubMessageType.Error, QueryId = queryId, Payload = errorMessage, SubscriptionGeneration = subscriptionGeneration };

    /// <summary>
    /// Creates a <see cref="ObservableQueryHubMessageType.Pong"/> message.
    /// </summary>
    /// <param name="timestamp">The timestamp echoed from the original ping.</param>
    /// <returns>A populated <see cref="ObservableQueryHubMessage"/>.</returns>
    public static ObservableQueryHubMessage CreatePong(long timestamp) =>
        new() { Type = ObservableQueryHubMessageType.Pong, Timestamp = timestamp };

    /// <summary>
    /// Creates a <see cref="ObservableQueryHubMessageType.Ping"/> message with the current UTC timestamp.
    /// </summary>
    /// <returns>A populated <see cref="ObservableQueryHubMessage"/>.</returns>
    public static ObservableQueryHubMessage CreatePing() =>
        new() { Type = ObservableQueryHubMessageType.Ping, Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };

    /// <summary>
    /// Creates a <see cref="ObservableQueryHubMessageType.Connected"/> message carrying the server-assigned
    /// connection identifier and the server's keep-alive interval.
    /// </summary>
    /// <param name="connectionId">The unique identifier for the SSE connection.</param>
    /// <param name="keepAliveInterval">The server's keep-alive interval. Zero or negative means keep-alive is disabled.</param>
    /// <returns>A populated <see cref="ObservableQueryHubMessage"/>.</returns>
    public static ObservableQueryHubMessage CreateConnected(string connectionId, TimeSpan keepAliveInterval) =>
        new()
        {
            Type = ObservableQueryHubMessageType.Connected,
            Payload = connectionId,
            KeepAliveIntervalMs = keepAliveInterval > TimeSpan.Zero ? (long)keepAliveInterval.TotalMilliseconds : 0
        };
}
