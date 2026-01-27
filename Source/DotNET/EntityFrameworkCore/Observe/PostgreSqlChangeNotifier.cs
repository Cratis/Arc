// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Npgsql;

namespace Cratis.Arc.EntityFrameworkCore.Observe;

/// <summary>
/// PostgreSQL implementation of <see cref="IDatabaseChangeNotifier"/> using LISTEN/NOTIFY.
/// </summary>
/// <param name="connectionString">The connection string to use.</param>
/// <param name="logger">The logger.</param>
public sealed class PostgreSqlChangeNotifier(string connectionString, ILogger<PostgreSqlChangeNotifier> logger) : IDatabaseChangeNotifier
{
    /// <summary>
    /// Debounce interval to prevent multiple notifications for rapid successive changes.
    /// </summary>
    static readonly TimeSpan _debounceInterval = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Delay before attempting to reconnect after a connection failure.
    /// </summary>
    static readonly TimeSpan _reconnectDelay = TimeSpan.FromSeconds(5);

    readonly object _lock = new();

    NpgsqlConnection? _connection;
    CancellationTokenSource? _listenerCts;
    Task? _listenerTask;
    string? _channelName;
    string? _tableName;
    Action? _onChanged;
    DateTime _lastNotification = DateTime.MinValue;
    bool _triggerCreated;
    bool _disposed;

    /// <inheritdoc/>
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities - channel name is derived from table name from EF Core metadata
    public async Task StartListening(string tableName, Action onChanged, CancellationToken cancellationToken = default)
    {
        _tableName = tableName;
        _channelName = $"table_change_{tableName.ToLowerInvariant()}";
        _onChanged = onChanged;

        await ConnectAndSubscribe(cancellationToken);

        // Start a background task to keep the connection alive and receive notifications
        _listenerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listenerTask = Task.Run(async () => await ListenLoopAsync(_listenerCts.Token), _listenerCts.Token);
    }
#pragma warning restore CA2100

    /// <inheritdoc/>
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities - channel name is derived from table name from EF Core metadata
    public async Task StopListening()
    {
        if (_listenerCts is not null)
        {
            await _listenerCts.CancelAsync();
            _listenerCts.Dispose();
            _listenerCts = null;
        }

        if (_listenerTask is not null)
        {
            try
            {
                await _listenerTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
            }
            _listenerTask = null;
        }

        await CleanupConnectionAsync();

        logger.StoppedListeningPostgreSql(_channelName ?? "unknown");
    }
#pragma warning restore CA2100

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

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities - channel name is derived from table name from EF Core metadata
    async Task ConnectAndSubscribe(CancellationToken cancellationToken)
    {
        _connection = new NpgsqlConnection(connectionString);
        await _connection.OpenAsync(cancellationToken);

        // Try to create trigger and notification function (may fail due to permissions)
        if (!_triggerCreated)
        {
            try
            {
                await EnsureTriggerExistsAsync(_tableName!, cancellationToken);
                _triggerCreated = true;
            }
            catch (PostgresException ex) when (ex.SqlState == "42501")
            {
                // Insufficient privilege
                logger.PostgreSqlTriggerPermissionDenied(_tableName!);
            }
            catch (Exception ex)
            {
                // Continue without trigger - user may have set it up manually
                logger.PostgreSqlTriggerCreationFailed(_tableName!, ex);
            }
        }

        // Subscribe to notifications
        _connection.Notification += OnNotification;

        await using var cmd = new NpgsqlCommand($"LISTEN {_channelName}", _connection);
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        logger.StartedListeningPostgreSql(_channelName!);
    }

    async Task CleanupConnectionAsync()
    {
        if (_connection is not null)
        {
            _connection.Notification -= OnNotification;
            if (_channelName is not null && _connection.State == System.Data.ConnectionState.Open)
            {
                try
                {
                    await using var cmd = new NpgsqlCommand($"UNLISTEN {_channelName}", _connection);
                    await cmd.ExecuteNonQueryAsync();
                }
                catch
                {
                    // Ignore errors during cleanup
                }
            }
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
#pragma warning restore CA2100

#pragma warning disable CA2100, MA0101, MA0136 // Review SQL queries for security vulnerabilities, Use raw string literal
    async Task EnsureTriggerExistsAsync(string tableName, CancellationToken cancellationToken)
    {
        var functionName = $"notify_{tableName.ToLowerInvariant()}_changes";
        var triggerName = $"trigger_{tableName.ToLowerInvariant()}_notify";

        // Create the notification function
        var createFunctionSql = $"""
            CREATE OR REPLACE FUNCTION {functionName}()
            RETURNS TRIGGER AS $$
            BEGIN
                PERFORM pg_notify('{_channelName}', TG_OP);
                RETURN COALESCE(NEW, OLD);
            END;
            $$ LANGUAGE plpgsql;
            """;

        await using var createFunctionCmd = new NpgsqlCommand(createFunctionSql, _connection);
        await createFunctionCmd.ExecuteNonQueryAsync(cancellationToken);

        // Create the trigger (drop first to avoid duplicates)
        var dropTriggerSql = $"""
            DROP TRIGGER IF EXISTS {triggerName} ON "{tableName}";
            """;

        await using var dropTriggerCmd = new NpgsqlCommand(dropTriggerSql, _connection);
        await dropTriggerCmd.ExecuteNonQueryAsync(cancellationToken);

        var createTriggerSql = $"""
            CREATE TRIGGER {triggerName}
            AFTER INSERT OR UPDATE OR DELETE ON "{tableName}"
            FOR EACH ROW EXECUTE FUNCTION {functionName}();
            """;

        await using var createTriggerCmd = new NpgsqlCommand(createTriggerSql, _connection);
        await createTriggerCmd.ExecuteNonQueryAsync(cancellationToken);

        logger.CreatedPostgreSqlTrigger(triggerName, tableName);
    }
#pragma warning restore CA2100, MA0101, MA0136

    async Task ListenLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && !_disposed)
        {
            try
            {
                if (_connection is null || _connection.State != System.Data.ConnectionState.Open)
                {
                    // Attempt to reconnect
                    logger.PostgreSqlReconnecting(_channelName ?? "unknown");
                    await CleanupConnectionAsync();
                    await Task.Delay(_reconnectDelay, cancellationToken);
                    await ConnectAndSubscribe(cancellationToken);
                }

                await _connection!.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (NpgsqlException ex)
            {
                logger.PostgreSqlListenerError(ex);

                // Will attempt to reconnect on next iteration
                await Task.Delay(_reconnectDelay, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.PostgreSqlListenerError(ex);
                break;
            }
        }
    }

    void OnNotification(object sender, NpgsqlNotificationEventArgs e)
    {
        if (e.Channel != _channelName)
        {
            return;
        }

        // Debounce to prevent rapid-fire notifications
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            if (now - _lastNotification < _debounceInterval)
            {
                return;
            }
            _lastNotification = now;
        }

        logger.ReceivedPostgreSqlNotification(e.Channel, e.Payload);
        _onChanged?.Invoke();
    }
}
