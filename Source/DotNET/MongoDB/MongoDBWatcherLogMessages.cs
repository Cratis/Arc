// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.MongoDB;

#pragma warning disable MA0048 // File name must match type name

internal static partial class MongoDBWatcherLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Started watching database '{DatabaseName}'")]
    internal static partial void StartedWatchingDatabase(this ILogger<MongoDBWatcher> logger, string databaseName);

    [LoggerMessage(LogLevel.Debug, "Database watch completed for '{DatabaseName}'")]
    internal static partial void DatabaseWatchCompleted(this ILogger<MongoDBWatcher> logger, string databaseName);

    [LoggerMessage(LogLevel.Debug, "Database watch was cancelled")]
    internal static partial void DatabaseWatchCancelled(this ILogger<MongoDBWatcher> logger);

    [LoggerMessage(LogLevel.Warning, "Unexpected error occurred in database watcher")]
    internal static partial void UnexpectedError(this ILogger<MongoDBWatcher> logger, Exception ex);
}
