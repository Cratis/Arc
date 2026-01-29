// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.EntityFrameworkCore.Observe;

/// <summary>
/// SQL Server implementation of <see cref="IDatabaseChangeNotifier"/> using SqlDependency.
/// </summary>
/// <param name="connectionString">The connection string to use.</param>
/// <param name="serviceBrokerManager">The Service Broker manager.</param>
/// <param name="logger">The logger.</param>
public sealed class SqlServerChangeNotifier(string connectionString, IServiceBrokerManager serviceBrokerManager, ILogger<SqlServerChangeNotifier> logger) : IDatabaseChangeNotifier
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
    string? _columnList;
    Action? _onChanged;
    CancellationToken _cancellationToken;
    CancellationTokenSource? _internalCancellationTokenSource;
    DateTime _lastNotification = DateTime.MinValue;
    bool _isSubscribing;
    bool _disposed;
    int _consecutiveFailures;

    /// <inheritdoc/>
    public async Task StartListening(string tableName, IEnumerable<string> columnNames, Action onChanged, CancellationToken cancellationToken = default)
    {
        _tableName = tableName;
        _columnList = string.Join(", ", columnNames.Select(c => $"[{c}]"));
        _onChanged = onChanged;
        _cancellationToken = cancellationToken;
        _internalCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Ensure Service Broker is enabled
        await serviceBrokerManager.EnsureEnabled(connectionString, cancellationToken);

        // Start SqlDependency
        SqlDependency.Start(connectionString);

        await SetupDependency();

        logger.StartedListeningSqlServer(tableName);
    }

    /// <inheritdoc/>
    public async Task StopListening()
    {
        if (_internalCancellationTokenSource is not null)
        {
            await _internalCancellationTokenSource.CancelAsync();
        }

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
            try
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
            }
            catch
            {
                // Ignore errors during cleanup
            }
            finally
            {
                _connection = null;
            }
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
        _internalCancellationTokenSource?.Dispose();
        _subscriptionSemaphore.Dispose();
    }

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities - table name comes from EF Core metadata
    async Task SetupDependency()
    {
        var effectiveCancellationToken = _internalCancellationTokenSource?.Token ?? _cancellationToken;
        if (effectiveCancellationToken.IsCancellationRequested || _disposed)
        {
            return;
        }

        // Prevent concurrent subscription attempts
        if (!await _subscriptionSemaphore.WaitAsync(0, effectiveCancellationToken))
        {
            return;
        }

        try
        {
            _isSubscribing = true;

            // Clean up previous dependency
            if (_dependency is not null)
            {
                _dependency.OnChange -= OnDependencyChange;
                _dependency = null;
            }

            // Create new connection if needed (don't reuse/dispose existing one during re-subscription)
            if (_connection is null || _connection.State != System.Data.ConnectionState.Open)
            {
                // Only dispose if we're creating a new one
                if (_connection is not null)
                {
                    try
                    {
                        await _connection.DisposeAsync();
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }

                _connection = new SqlConnection(connectionString);
                await _connection.OpenAsync(effectiveCancellationToken);
            }

            // SqlDependency requires a specific query format
            // Must use two-part name (schema.table) and specific columns (no *)
            // We select all columns to ensure notifications are received for any column change
            var sql = $"SELECT {_columnList} FROM dbo.[{_tableName}]";

            // Set the required SET options for SqlDependency
            // These are REQUIRED by SQL Server Query Notifications
            const string setOptions = "SET ARITHABORT ON; " +
                                      "SET CONCAT_NULL_YIELDS_NULL ON; " +
                                      "SET QUOTED_IDENTIFIER ON; " +
                                      "SET ANSI_NULLS ON; " +
                                      "SET ANSI_PADDING ON; " +
                                      "SET ANSI_WARNINGS ON; " +
                                      "SET NUMERIC_ROUNDABORT OFF;";

            await using (var setOptionsCommand = new SqlCommand(setOptions, _connection))
            {
                await setOptionsCommand.ExecuteNonQueryAsync(effectiveCancellationToken);
            }

            await using var command = new SqlCommand(sql, _connection);
            command.CommandTimeout = 30;

            logger.SqlServerSettingUpDependency(_tableName ?? "unknown", sql);

            _dependency = new SqlDependency(command);
            _dependency.OnChange += OnDependencyChange;

            logger.SqlServerDependencyCreated(_dependency.Id, _dependency.HasChanges);

            // Execute the query to register the dependency
            // Do NOT use 'await using' - it will dispose the connection!
            // SqlDependency requires the connection to stay open to receive notifications
#pragma warning disable RCS1261 // Resource can be disposed asynchronously
#pragma warning disable MA0042 // Prefer using 'await using'
            var reader = await command.ExecuteReaderAsync(effectiveCancellationToken);
            try
            {
                // Read all data to complete the registration
                while (await reader.ReadAsync(effectiveCancellationToken))
                {
                    // Just reading to register the dependency
                }
            }
            finally
            {
                // Manually dispose reader but NOT the connection
                await reader.DisposeAsync();
            }
#pragma warning restore MA0042, RCS1261

            // Reset failure counter on success
            _consecutiveFailures = 0;
            logger.SqlServerDependencySetupSuccess(_tableName ?? "unknown");
        }
        catch (Exception ex)
        {
            _consecutiveFailures++;
            logger.SqlServerDependencySetupFailed(_tableName ?? "unknown", ex, _consecutiveFailures);

            // If we're having repeated failures, increase the retry delay
            if (!effectiveCancellationToken.IsCancellationRequested && !_disposed)
            {
                var retryDelay = TimeSpan.FromMilliseconds(Math.Min(5000, 500 * _consecutiveFailures));
                _ = Task.Run(
                    async () =>
                    {
                        try
                        {
                            await Task.Delay(retryDelay, effectiveCancellationToken);
                            await SetupDependency();
                        }
                        catch
                        {
                            // Ignore retry errors
                        }
                    },
                    effectiveCancellationToken);
            }
            throw;
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
        // ALWAYS log all notifications for debugging
        logger.ReceivedSqlServerNotification(e.Type.ToString(), e.Info.ToString(), e.Source.ToString());

        var effectiveCancellationToken = _internalCancellationTokenSource?.Token ?? _cancellationToken;

        // Check for errors in the notification
        if (e.Info == SqlNotificationInfo.Error)
        {
            logger.SqlServerNotificationError(e.Type.ToString(), e.Info.ToString(), e.Source.ToString());
        }

        // Also check for invalid notifications that indicate SqlDependency couldn't subscribe properly
        else if (e.Info == SqlNotificationInfo.Invalid)
        {
            logger.SqlServerNotificationInvalid(e.Type.ToString(), e.Info.ToString(), e.Source.ToString());
        }

        // Notify on actual data changes
        // Note: SqlDependency fires with Type=Change for ANY change, including subscribes/unsubscribes
        // We need to check Info to determine if it's an actual data change
        var isDataChange = e.Type == SqlNotificationType.Change &&
                          e.Info != SqlNotificationInfo.Error &&
                          e.Info != SqlNotificationInfo.Invalid &&
                          (e.Info == SqlNotificationInfo.Insert ||
                           e.Info == SqlNotificationInfo.Update ||
                           e.Info == SqlNotificationInfo.Delete);

        if (isDataChange)
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
                try
                {
                    _onChanged?.Invoke();
                }
                catch (Exception ex)
                {
                    logger.OnChangedCallbackError(ex);
                }
            }
        }

        // Re-subscribe after receiving a notification (SqlDependency is one-shot)
        if (!effectiveCancellationToken.IsCancellationRequested && !_disposed && !_isSubscribing)
        {
            _ = Task.Run(
                async () =>
                {
                    try
                    {
                        // Add delay before re-subscribing to prevent spin
                        await Task.Delay(_resubscribeDelay, effectiveCancellationToken);
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
                effectiveCancellationToken);
        }
    }
}
