// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore;

#pragma warning disable MA0048 // File name must match type name

internal static partial class DbSetObserveExtensionsLogMessages
{
    [LoggerMessage(LogLevel.Trace, "Starting observation for entity type {EntityType}")]
    internal static partial void StartingObservation(this ILogger<DbSetObserveExtensions.DbSetObserver> logger, string entityType);

    [LoggerMessage(LogLevel.Trace, "Change detected, re-querying for entity type {EntityType}")]
    internal static partial void ChangeDetectedRequerying(this ILogger<DbSetObserveExtensions.DbSetObserver> logger, string entityType);

    [LoggerMessage(LogLevel.Trace, "Observation completed for entity type {EntityType}")]
    internal static partial void ObservationCompleted(this ILogger<DbSetObserveExtensions.DbSetObserver> logger, string entityType);

    [LoggerMessage(LogLevel.Trace, "Object was disposed")]
    internal static partial void ObjectDisposed(this ILogger<DbSetObserveExtensions.DbSetObserver> logger);

    [LoggerMessage(LogLevel.Trace, "Operation was cancelled")]
    internal static partial void OperationCancelled(this ILogger<DbSetObserveExtensions.DbSetObserver> logger);

    [LoggerMessage(LogLevel.Trace, "Cleaning up observation")]
    internal static partial void CleaningUp(this ILogger<DbSetObserveExtensions.DbSetObserver> logger);

    [LoggerMessage(LogLevel.Warning, "Unexpected error occurred during observation for entity type {EntityType}")]
    internal static partial void UnexpectedError(this ILogger<DbSetObserveExtensions.DbSetObserver> logger, string entityType, Exception ex);

    [LoggerMessage(LogLevel.Debug, "Database notifier started for entity type {EntityType}")]
    internal static partial void DatabaseNotifierStarted(this ILogger<DbSetObserveExtensions.DbSetObserver> logger, string entityType);

    [LoggerMessage(LogLevel.Warning, "Failed to start database notifier for entity type {EntityType}, falling back to in-process only")]
    internal static partial void DatabaseNotifierFailed(this ILogger<DbSetObserveExtensions.DbSetObserver> logger, string entityType, Exception ex);

    [LoggerMessage(LogLevel.Debug, "Skipping query for entity type {EntityType} - cleanup in progress")]
    internal static partial void SkippingQueryDuringCleanup(this ILogger<DbSetObserveExtensions.DbSetObserver> logger, string entityType);

    [LoggerMessage(LogLevel.Information, "Observation for entity type {EntityType} is now watching for changes")]
    internal static partial void ObservationWatchingForChanges(this ILogger<DbSetObserveExtensions.DbSetObserver> logger, string entityType);

    [LoggerMessage(LogLevel.Information, "Watch task ending for entity type {EntityType}")]
    internal static partial void WatchTaskEnding(this ILogger<DbSetObserveExtensions.DbSetObserver> logger, string entityType);
}
