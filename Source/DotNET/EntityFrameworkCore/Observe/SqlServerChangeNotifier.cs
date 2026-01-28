// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.EntityFrameworkCore.Observe;

/// <summary>
/// SQL Server implementation of <see cref="IDatabaseChangeNotifier"/> using SqlDependency.
/// </summary>
/// <param name="connectionString">The connection string to use.</param>
/// <param name="logger">The logger.</param>
public sealed class SqlServerChangeNotifier(string connectionString, ILogger<SqlServerChangeNotifier> logger) : IDatabaseChangeNotifier
{
    /// <summary>
    /// Delay before re-subscribing after receiving a notification to prevent spin.
    /// </summary>
    static readonly TimeSpan _resubscribeDelay = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Debounce interval to prevent multiple notifications for rapid successive changes.
    /// </summary>
    static readonly TimeSpan _debounceInterval = TimeSpan.FromMilliseconds(50);

    readonly object _lock = new();
    readonly SemaphoreSlim _subscriptionSemaphore = new(1, 1);

    SqlConnection? _connection;
    SqlDependency? _dependency;
    string? _tableName;
    Action? _onChanged;
    CancellationToken _cancellationToken;
    DateTime _lastNotification = DateTime.MinValue;
    bool _isSubscribing;
    bool _disposed;

    /// <inheritdoc/>
    public async Task StartListening(string tableName, Action onChanged, CancellationToken cancellationToken = default)
    {
        _tableName = tableName;
        _onChanged = onChanged;
        _cancellationToken = cancellationToken;

        // Start SqlDependency
        SqlDependency.Start(connectionString);

        await SetupDependency();

        logger.StartedListeningSqlServer(tableName);
    }

    /// <inheritdoc/>
    public async Task StopListening()
    {
        try
        {
            SqlDependency.Stop(connectionString);
        }
        catch
        {
            // Ignore errors during cleanup
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }

        logger.StoppedListeningSqlServer(_tableName ?? "unknown");
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
        _subscriptionSemaphore.Dispose();
    }

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities - table name comes from EF Core metadata
    async Task SetupDependency()
    {
        if (_cancellationToken.IsCancellationRequested || _disposed)
        {
            return;
        }

        // Prevent concurrent subscription attempts
        if (!await _subscriptionSemaphore.WaitAsync(0, _cancellationToken))
        {
            return;
        }

        try
        {
            _isSubscribing = true;

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }
            _connection = new SqlConnection(connectionString);
            await _connection.OpenAsync(_cancellationToken);

            // SqlDependency requires a specific query format
            // Using a minimal SELECT to reduce overhead
            var sql = $"SELECT Id FROM dbo.[{_tableName}]";
            await using var command = new SqlCommand(sql, _connection);

            _dependency = new SqlDependency(command);
            _dependency.OnChange += OnDependencyChange;

            // Execute the query to register the dependency
            await using var reader = await command.ExecuteReaderAsync(_cancellationToken);

            // Read all data to complete the registration
            while (await reader.ReadAsync(_cancellationToken))
            {
                // Just reading to register the dependency
            }
        }
        finally
        {
            _isSubscribing = false;
            _subscriptionSemaphore.Release();
        }
    }
#pragma warning restore CA2100

    void OnDependencyChange(object sender, SqlNotificationEventArgs e)
    {
        logger.ReceivedSqlServerNotification(e.Type.ToString(), e.Info.ToString(), e.Source.ToString());

        // Only notify on actual data changes, not on subscription events
        if (e.Type == SqlNotificationType.Change)
        {
            // Debounce to prevent rapid-fire notifications
            bool shouldNotify;
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                shouldNotify = now - _lastNotification >= _debounceInterval;
                if (shouldNotify)
                {
                    _lastNotification = now;
                }
            }

            if (shouldNotify)
            {
                _onChanged?.Invoke();
            }
        }

        // Re-subscribe after receiving a notification (SqlDependency is one-shot)
        if (!_cancellationToken.IsCancellationRequested && !_disposed && !_isSubscribing)
        {
            _ = Task.Run(
                async () =>
                {
                    try
                    {
                        // Add delay before re-subscribing to prevent spin
                        await Task.Delay(_resubscribeDelay, _cancellationToken);
                        await SetupDependency();
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when stopping
                    }
                    catch (Exception ex)
                    {
                        logger.SqlServerResubscribeError(ex);
                    }
                },
                _cancellationToken);
        }
    }
}
