// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents a protocol message exchanged over the <see cref="ObservableQueryDemultiplexer"/> WebSocket or SSE connection.
/// </summary>
public class ObservableQueryHubMessage
{
    /// <summary>
    /// Gets or sets the type of message.
    /// </summary>
    public ObservableQueryHubMessageType Type { get; set; }

    /// <summary>
    /// Gets or sets the client-assigned query identifier used to correlate subscriptions with their result updates.
    /// </summary>
    public string? QueryId { get; set; }

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
    /// Creates a <see cref="ObservableQueryHubMessageType.QueryResult"/> message.
    /// </summary>
    /// <param name="queryId">The query identifier.</param>
    /// <param name="result">The query result payload.</param>
    /// <returns>A populated <see cref="ObservableQueryHubMessage"/>.</returns>
    public static ObservableQueryHubMessage CreateQueryResult(string queryId, QueryResult result) =>
        new() { Type = ObservableQueryHubMessageType.QueryResult, QueryId = queryId, Payload = result };

    /// <summary>
    /// Creates an <see cref="ObservableQueryHubMessageType.Unauthorized"/> message.
    /// </summary>
    /// <param name="queryId">The query identifier.</param>
    /// <returns>A populated <see cref="ObservableQueryHubMessage"/>.</returns>
    public static ObservableQueryHubMessage CreateUnauthorized(string queryId) =>
        new() { Type = ObservableQueryHubMessageType.Unauthorized, QueryId = queryId };

    /// <summary>
    /// Creates an <see cref="ObservableQueryHubMessageType.Error"/> message.
    /// </summary>
    /// <param name="queryId">The query identifier.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A populated <see cref="ObservableQueryHubMessage"/>.</returns>
    public static ObservableQueryHubMessage CreateError(string queryId, string errorMessage) =>
        new() { Type = ObservableQueryHubMessageType.Error, QueryId = queryId, Payload = errorMessage };

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
    /// Creates a <see cref="ObservableQueryHubMessageType.Connected"/> message carrying the server-assigned connection identifier.
    /// </summary>
    /// <param name="connectionId">The unique identifier for the SSE connection.</param>
    /// <returns>A populated <see cref="ObservableQueryHubMessage"/>.</returns>
    public static ObservableQueryHubMessage CreateConnected(string connectionId) =>
        new() { Type = ObservableQueryHubMessageType.Connected, Payload = connectionId };
}
