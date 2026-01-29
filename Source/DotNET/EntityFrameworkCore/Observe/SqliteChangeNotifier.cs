// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.EntityFrameworkCore.Observe;

/// <summary>
/// SQLite implementation of <see cref="IDatabaseChangeNotifier"/> using the native update_hook.
/// This provides per-table change notifications for all operations (including raw SQL).
/// </summary>
/// <param name="connectionString">The connection string to the SQLite database.</param>
/// <param name="logger">The logger.</param>
public sealed class SqliteChangeNotifier(string connectionString, ILogger<SqliteChangeNotifier> logger) : IDatabaseChangeNotifier
{
    /// <summary>
    /// Debounce interval to prevent multiple notifications for rapid successive changes.
    /// </summary>
    static readonly TimeSpan _debounceInterval = TimeSpan.FromMilliseconds(50);

    readonly object _lock = new();
    SqliteConnection? _connection;
    SqliteUpdateHook.UpdateHookCallback? _callback;
    string? _tableName;
    Action? _onChanged;
    DateTime _lastNotification = DateTime.MinValue;
    bool _disposed;

    /// <inheritdoc/>
    public async Task StartListening(string tableName, IEnumerable<string> columnNames, Action onChanged, CancellationToken cancellationToken = default)
    {
        _tableName = tableName;
        _onChanged = onChanged;

        // Open a dedicated connection for the update hook
        // The hook is per-connection, so we need to keep this connection open
        _connection = new SqliteConnection(connectionString);
        await _connection.OpenAsync(cancellationToken);

        // Keep a reference to the callback delegate to prevent garbage collection
        _callback = OnUpdateHook;

        // Get the underlying SQLite handle
        var handle = GetSqliteHandle(_connection);
        if (handle == IntPtr.Zero)
        {
            logger.SqliteHandleNotFound();
            return;
        }

        // Register the update hook
        SqliteUpdateHook.RegisterUpdateHook(handle, _callback, IntPtr.Zero);

        logger.StartedListeningSqlite(tableName);
    }

    /// <inheritdoc/>
    public async Task StopListening()
    {
        if (_connection is not null)
        {
            // Unregister the hook by passing null
            var handle = GetSqliteHandle(_connection);
            if (handle != IntPtr.Zero)
            {
                SqliteUpdateHook.RegisterUpdateHook(handle, null, IntPtr.Zero);
            }

            await _connection.CloseAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }

        _callback = null;
        logger.StoppedListeningSqlite(_tableName ?? "unknown");
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await StopListening();
    }

    static IntPtr GetSqliteHandle(SqliteConnection connection)
    {
        // Access the internal Handle property via reflection
        // Microsoft.Data.Sqlite exposes the handle through the DbConnection
        var handleProperty = typeof(SqliteConnection)
            .GetProperty("Handle", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        if (handleProperty is null)
        {
            return IntPtr.Zero;
        }

        var handleObject = handleProperty.GetValue(connection);
        if (handleObject is null)
        {
            return IntPtr.Zero;
        }

        // The Handle is a SafeHandle, we need to get the DangerousGetHandle value
        var dangerousMethod = handleObject.GetType()
            .GetMethod("DangerousGetHandle", BindingFlags.Instance | BindingFlags.Public);

        if (dangerousMethod is null)
        {
            return IntPtr.Zero;
        }

        return (IntPtr)(dangerousMethod.Invoke(handleObject, null) ?? IntPtr.Zero);
    }

    void OnUpdateHook(IntPtr userData, int action, string databaseName, string tableName, long rowId)
    {
        // Only notify for the specific table we're watching
        if (!string.Equals(tableName, _tableName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Debounce to prevent multiple notifications for rapid changes
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            if (now - _lastNotification < _debounceInterval)
            {
                return;
            }
            _lastNotification = now;
        }

        var actionType = (SqliteUpdateHook.SqliteAction)action;
        logger.SqliteUpdateHookTriggered(tableName, actionType.ToString(), rowId);

        // Invoke on a separate thread to avoid blocking the SQLite callback
        ThreadPool.QueueUserWorkItem(_ => _onChanged?.Invoke());
    }
}
