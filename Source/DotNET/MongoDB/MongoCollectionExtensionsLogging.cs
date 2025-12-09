// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace MongoDB.Driver;

#pragma warning disable MA0048 // File name must match type name

internal static partial class MongoCollectionExtensionsLogMessages
{
    [LoggerMessage(LogLevel.Trace, "Client unsubscribed")]
    internal static partial void ClientUnsubscribed(this ILogger<MongoCollectionExtensions.MongoCollection> logger);

    [LoggerMessage(LogLevel.Trace, "Iterating change stream cursor completed")]
    internal static partial void IteratingChangeStreamCursorCompleted(this ILogger<MongoCollectionExtensions.MongoCollection> logger);

    [LoggerMessage(LogLevel.Trace, "Object was disposed")]
    internal static partial void ObjectDisposed(this ILogger<MongoCollectionExtensions.MongoCollection> logger);

    [LoggerMessage(LogLevel.Trace, "Operation was cancelled")]
    internal static partial void OperationCancelled(this ILogger<MongoCollectionExtensions.MongoCollection> logger);

    [LoggerMessage(LogLevel.Trace, "Cleaning up")]
    internal static partial void CleaningUp(this ILogger<MongoCollectionExtensions.MongoCollection> logger);

    [LoggerMessage(LogLevel.Warning, "Unexpected error occurred")]
    internal static partial void UnexpectedError(this ILogger<MongoCollectionExtensions.MongoCollection> logger, Exception ex);
}