// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.EntityFrameworkCore.Observe;

internal static partial class ObserveInterceptorLogging
{
    [LoggerMessage(LogLevel.Debug, "CaptureChangedTables called with null context")]
    internal static partial void CaptureChangedTablesNullContext(this ILogger<ObserveInterceptor> logger);

    [LoggerMessage(LogLevel.Debug, "CaptureChangedTables captured {Count} tables: {Tables}")]
    internal static partial void CapturedChangedTables(this ILogger<ObserveInterceptor> logger, int count, string tables);

    [LoggerMessage(LogLevel.Debug, "NotifyChanges called with {Count} tables: {Tables}")]
    internal static partial void NotifyingChanges(this ILogger<ObserveInterceptor> logger, int count, string tables);

    [LoggerMessage(LogLevel.Debug, "Notifying change for table: {TableName}")]
    internal static partial void NotifyingChangeForTable(this ILogger<ObserveInterceptor> logger, string tableName);
}
