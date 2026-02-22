// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.EntityFrameworkCore.Observe;

/// <summary>
/// SQLite implementation of <see cref="IDatabaseChangeNotifier"/>.
/// SQLite does not support cross-connection change notifications.
/// This implementation is a no-op and relies solely on in-process change detection via ObserveInterceptor.
/// </summary>
/// <param name="logger">The logger.</param>
public sealed class SqliteChangeNotifier(ILogger<SqliteChangeNotifier> logger) : IDatabaseChangeNotifier
{
    bool _disposed;

    /// <inheritdoc/>
    public Task StartListening(string tableName, string? schemaName, IEnumerable<string> columnNames, Action onChanged, CancellationToken cancellationToken = default)
    {
        logger.SqliteUsingInProcessOnly(tableName);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopListening()
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;
        return ValueTask.CompletedTask;
    }
}
