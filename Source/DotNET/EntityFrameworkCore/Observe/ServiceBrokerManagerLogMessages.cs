// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.EntityFrameworkCore.Observe;

#pragma warning disable MA0048 // File name must match type name

internal static partial class ServiceBrokerManagerLogMessages
{
    [LoggerMessage(LogLevel.Information, "Service Broker is enabled on the database")]
    internal static partial void ServiceBrokerEnabled(this ILogger<ServiceBrokerManager> logger);

    [LoggerMessage(LogLevel.Error, "Service Broker is NOT enabled on the database - SqlDependency will not work. Enable with: ALTER DATABASE [YourDB] SET ENABLE_BROKER")]
    internal static partial void ServiceBrokerDisabled(this ILogger<ServiceBrokerManager> logger);

    [LoggerMessage(LogLevel.Warning, "Failed to check Service Broker status")]
    internal static partial void ServiceBrokerCheckFailed(this ILogger<ServiceBrokerManager> logger, Exception ex);

    [LoggerMessage(LogLevel.Information, "Attempting to enable Service Broker for database: {DatabaseKey}")]
    internal static partial void AttemptingToEnableServiceBroker(this ILogger<ServiceBrokerManager> logger, string databaseKey);

    [LoggerMessage(LogLevel.Information, "Successfully enabled Service Broker for database: {DatabaseKey}")]
    internal static partial void ServiceBrokerEnabledSuccessfully(this ILogger<ServiceBrokerManager> logger, string databaseKey);

    [LoggerMessage(LogLevel.Warning, "Failed to enable Service Broker for database: {DatabaseKey} - verification failed")]
    internal static partial void ServiceBrokerEnablementFailed(this ILogger<ServiceBrokerManager> logger, string databaseKey);

    [LoggerMessage(LogLevel.Warning, "Cannot enable Service Broker for database: {DatabaseKey} - database is in use by other connections. Close connections or run manually: ALTER DATABASE [DB] SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE")]
    internal static partial void ServiceBrokerEnablementFailedDatabaseInUse(this ILogger<ServiceBrokerManager> logger, string databaseKey, Exception ex);

    [LoggerMessage(LogLevel.Warning, "Cannot enable Service Broker for database: {DatabaseKey} - insufficient permissions. Requires ALTER DATABASE permission. Enable manually or grant permissions.")]
    internal static partial void ServiceBrokerEnablementFailedPermissions(this ILogger<ServiceBrokerManager> logger, string databaseKey, Exception ex);

    [LoggerMessage(LogLevel.Error, "Unexpected error enabling Service Broker for database: {DatabaseKey}")]
    internal static partial void ServiceBrokerEnablementFailedUnexpected(this ILogger<ServiceBrokerManager> logger, string databaseKey, Exception ex);
}
