// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.EntityFrameworkCore.Observe;

#pragma warning disable MA0048 // File name must match type name

internal static partial class DatabaseChangeNotifierLogMessages
{
    // PostgreSQL
    [LoggerMessage(LogLevel.Information, "Started listening for PostgreSQL notifications on channel {ChannelName}")]
    internal static partial void StartedListeningPostgreSql(this ILogger<PostgreSqlChangeNotifier> logger, string channelName);

    [LoggerMessage(LogLevel.Information, "Stopped listening for PostgreSQL notifications on channel {ChannelName}")]
    internal static partial void StoppedListeningPostgreSql(this ILogger<PostgreSqlChangeNotifier> logger, string channelName);

    [LoggerMessage(LogLevel.Debug, "Created PostgreSQL trigger {TriggerName} on table {TableName}")]
    internal static partial void CreatedPostgreSqlTrigger(this ILogger<PostgreSqlChangeNotifier> logger, string triggerName, string tableName);

    [LoggerMessage(LogLevel.Debug, "Received PostgreSQL notification on channel {Channel} with payload {Payload}")]
    internal static partial void ReceivedPostgreSqlNotification(this ILogger<PostgreSqlChangeNotifier> logger, string channel, string payload);

    [LoggerMessage(LogLevel.Error, "PostgreSQL listener encountered an error")]
    internal static partial void PostgreSqlListenerError(this ILogger<PostgreSqlChangeNotifier> logger, Exception ex);

    [LoggerMessage(LogLevel.Warning, "Insufficient privileges to create trigger on table {TableName} - ensure trigger exists manually")]
    internal static partial void PostgreSqlTriggerPermissionDenied(this ILogger<PostgreSqlChangeNotifier> logger, string tableName);

    [LoggerMessage(LogLevel.Warning, "Failed to create trigger on table {TableName} - ensure trigger exists manually")]
    internal static partial void PostgreSqlTriggerCreationFailed(this ILogger<PostgreSqlChangeNotifier> logger, string tableName, Exception ex);

    [LoggerMessage(LogLevel.Information, "Attempting to reconnect PostgreSQL listener for channel {ChannelName}")]
    internal static partial void PostgreSqlReconnecting(this ILogger<PostgreSqlChangeNotifier> logger, string channelName);

    // SQL Server
    [LoggerMessage(LogLevel.Information, "Started listening for SQL Server notifications on table {TableName}")]
    internal static partial void StartedListeningSqlServer(this ILogger<SqlServerChangeNotifier> logger, string tableName);

    [LoggerMessage(LogLevel.Information, "Stopped listening for SQL Server notifications on table {TableName}")]
    internal static partial void StoppedListeningSqlServer(this ILogger<SqlServerChangeNotifier> logger, string tableName);

    [LoggerMessage(LogLevel.Information, "Received SQL Server notification: Type={Type}, Info={Info}, Source={Source}")]
    internal static partial void ReceivedSqlServerNotification(this ILogger<SqlServerChangeNotifier> logger, string type, string info, string source);

    [LoggerMessage(LogLevel.Error, "Error re-subscribing to SQL Server notifications")]
    internal static partial void SqlServerResubscribeError(this ILogger<SqlServerChangeNotifier> logger, Exception ex);

    [LoggerMessage(LogLevel.Information, "Setting up SQL Server dependency for table {TableName} with query: {Query}")]
    internal static partial void SqlServerSettingUpDependency(this ILogger<SqlServerChangeNotifier> logger, string tableName, string query);

    [LoggerMessage(LogLevel.Debug, "Successfully set up SQL Server dependency for table {TableName}")]
    internal static partial void SqlServerDependencySetupSuccess(this ILogger<SqlServerChangeNotifier> logger, string tableName);

    [LoggerMessage(LogLevel.Warning, "Failed to set up SQL Server dependency for table {TableName} (attempt {ConsecutiveFailures})")]
    internal static partial void SqlServerDependencySetupFailed(this ILogger<SqlServerChangeNotifier> logger, string tableName, Exception ex, int consecutiveFailures);

    [LoggerMessage(LogLevel.Error, "Error in OnChanged callback")]
    internal static partial void OnChangedCallbackError(this ILogger<SqlServerChangeNotifier> logger, Exception ex);

    [LoggerMessage(LogLevel.Warning, "SQL Server notification error: Type={Type}, Info={Info}, Source={Source}")]
    internal static partial void SqlServerNotificationError(this ILogger<SqlServerChangeNotifier> logger, string type, string info, string source);

    [LoggerMessage(LogLevel.Warning, "SQL Server notification invalid (SqlDependency couldn't subscribe): Type={Type}, Info={Info}, Source={Source}")]
    internal static partial void SqlServerNotificationInvalid(this ILogger<SqlServerChangeNotifier> logger, string type, string info, string source);

    // SQLite
    [LoggerMessage(LogLevel.Information, "Started listening for SQLite changes on table {TableName}")]
    internal static partial void StartedListeningSqlite(this ILogger<SqliteChangeNotifier> logger, string tableName);

    [LoggerMessage(LogLevel.Information, "Stopped listening for SQLite changes on table {TableName}")]
    internal static partial void StoppedListeningSqlite(this ILogger<SqliteChangeNotifier> logger, string tableName);

    [LoggerMessage(LogLevel.Debug, "SQLite update hook triggered: Table={TableName}, Action={Action}, RowId={RowId}")]
    internal static partial void SqliteUpdateHookTriggered(this ILogger<SqliteChangeNotifier> logger, string tableName, string action, long rowId);

    [LoggerMessage(LogLevel.Warning, "Could not obtain SQLite connection handle for update hook registration")]
    internal static partial void SqliteHandleNotFound(this ILogger<SqliteChangeNotifier> logger);
}
