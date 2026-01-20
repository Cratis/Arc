// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

internal static partial class ClientObservableLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Error sending message to client")]
    internal static partial void ObservableErrorSendingMessage(this ILogger<IClientObservable> logger, Exception error);

    [LoggerMessage(LogLevel.Debug, "WebSocket error while receiving messages from client")]
    internal static partial void ObservableWebSocketErrorReceivingMessage(this ILogger<IClientObservable> logger, Exception error);

    [LoggerMessage(LogLevel.Warning, "Error while receiving messages from client")]
    internal static partial void ObservableErrorReceivingMessage(this ILogger<IClientObservable> logger, Exception error);

    [LoggerMessage(LogLevel.Debug, "Client disconnected")]
    internal static partial void ObservableClientDisconnected(this ILogger<IClientObservable> logger);

    [LoggerMessage(LogLevel.Trace, "Sending message")]
    internal static partial void ObservableSendingMessage(this ILogger<IClientObservable> logger);

    [LoggerMessage(LogLevel.Trace, "Received message")]
    internal static partial void ObservableReceivedMessage(this ILogger<IClientObservable> logger);

    [LoggerMessage(LogLevel.Debug, "Closing connection. Description: {Description}")]
    internal static partial void ObservableCloseConnection(this ILogger<IClientObservable> logger, string? description);

    [LoggerMessage(LogLevel.Debug, "Could not send item to client. Skipping")]
    internal static partial void EnumerableObservableSkip(this ILogger<IClientObservable> logger);

    [LoggerMessage(LogLevel.Debug, "Received null-item. Waiting for next")]
    internal static partial void ObservableReceivedNullItem(this ILogger<IClientObservable> logger);

    [LoggerMessage(LogLevel.Warning, "Error while processing item from server. Cancelling the connection")]
    internal static partial void EnumerableObservableError(this ILogger<IClientObservable> logger, Exception ex);

    [LoggerMessage(LogLevel.Warning, "An error occurred")]
    internal static partial void ObservableAnErrorOccurred(this ILogger<IClientObservable> logger, Exception exception);

    [LoggerMessage(LogLevel.Debug, "The observed stream from the server completed")]
    internal static partial void ObservableCompleted(this ILogger<IClientObservable> logger);
}