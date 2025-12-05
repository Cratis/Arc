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
    NpgsqlConnection? _connection;
    CancellationTokenSource? _listenerCts;
    Task? _listenerTask;
    string? _channelName;
    Action? _onChanged;
    bool _disposed;

    /// <inheritdoc/>
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities - channel name is derived from table name from EF Core metadata
    public async Task StartListeningAsync(string tableName, Action onChanged, CancellationToken cancellationToken = default)
    {
        _channelName = $"table_change_{tableName.ToLowerInvariant()}";
        _onChanged = onChanged;

        _connection = new NpgsqlConnection(connectionString);
        await _connection.OpenAsync(cancellationToken);

        // Create trigger and notification function if they don't exist
        await EnsureTriggerExistsAsync(tableName, cancellationToken);

        // Subscribe to notifications
        _connection.Notification += OnNotification;

        await using var cmd = new NpgsqlCommand($"LISTEN {_channelName}", _connection);
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        logger.StartedListeningPostgreSql(_channelName);

        // Start a background task to keep the connection alive and receive notifications
        _listenerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listenerTask = Task.Run(async () => await ListenLoopAsync(_listenerCts.Token), _listenerCts.Token);
    }
#pragma warning restore CA2100

    /// <inheritdoc/>
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities - channel name is derived from table name from EF Core metadata
    public async Task StopListeningAsync()
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

        if (_connection is not null)
        {
            _connection.Notification -= OnNotification;
            if (_channelName is not null)
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
        await StopListeningAsync();
    }

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
                RETURN NEW;
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
        while (!cancellationToken.IsCancellationRequested && _connection is not null)
        {
            try
            {
                await _connection.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
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
        if (e.Channel == _channelName)
        {
            logger.ReceivedPostgreSqlNotification(e.Channel, e.Payload);
            _onChanged?.Invoke();
        }
    }
}
