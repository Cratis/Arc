// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using static MongoDB.Driver.MongoCollectionExtensions;

namespace Cratis.Arc.MongoDB;

#pragma warning disable MA0048 // File name must match type name

internal static partial class MongoCollectionExtensionsLogMessages
{
    [LoggerMessage(LogLevel.Trace, "Client unsubscribed")]
    internal static partial void ClientUnsubscribed(this ILogger<MongoCollection> logger);

    [LoggerMessage(LogLevel.Trace, "Iterating change stream cursor completed")]
    internal static partial void IteratingChangeStreamCursorCompleted(this ILogger<MongoCollection> logger);

    [LoggerMessage(LogLevel.Trace, "Object was disposed")]
    internal static partial void ObjectDisposed(this ILogger<MongoCollection> logger);

    [LoggerMessage(LogLevel.Trace, "Operation was cancelled")]
    internal static partial void OperationCancelled(this ILogger<MongoCollection> logger);

    [LoggerMessage(LogLevel.Trace, "Cleaning up")]
    internal static partial void CleaningUp(this ILogger<MongoCollection> logger);

    [LoggerMessage(LogLevel.Error, "Unexpected error observing collection {Collection} of {DocumentType}")]
    internal static partial void UnexpectedError(this ILogger<MongoCollection> logger, string collection, string? documentType, Exception ex);
}