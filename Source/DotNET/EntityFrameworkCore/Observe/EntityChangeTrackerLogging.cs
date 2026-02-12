// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.EntityFrameworkCore.Observe;

internal static partial class EntityChangeTrackerLogging
{
    [LoggerMessage(LogLevel.Debug, "Registered callback for table {TableName}, subscription {SubscriptionId}. Total callbacks for table: {Count}")]
    internal static partial void RegisteredCallback(this ILogger<EntityChangeTracker> logger, string tableName, Guid subscriptionId, int count);

    [LoggerMessage(LogLevel.Debug, "NotifyChange called for table {TableName}")]
    internal static partial void NotifyChangeCalled(this ILogger<EntityChangeTracker> logger, string tableName);

    [LoggerMessage(LogLevel.Debug, "No callbacks registered for table {TableName}")]
    internal static partial void NoCallbacksRegistered(this ILogger<EntityChangeTracker> logger, string tableName);

    [LoggerMessage(LogLevel.Debug, "Found {Count} callbacks for table {TableName}")]
    internal static partial void FoundCallbacks(this ILogger<EntityChangeTracker> logger, int count, string tableName);

    [LoggerMessage(LogLevel.Debug, "Invoking callback for table {TableName}")]
    internal static partial void InvokingCallback(this ILogger<EntityChangeTracker> logger, string tableName);

    [LoggerMessage(LogLevel.Debug, "Disposed callback for table {TableName}, subscription {SubscriptionId}. Remaining callbacks: {Count}")]
    internal static partial void DisposedCallback(this ILogger<EntityChangeTracker> logger, string tableName, Guid subscriptionId, int count);
}
