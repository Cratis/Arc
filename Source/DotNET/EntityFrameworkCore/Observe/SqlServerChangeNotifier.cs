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

    string? _normalizedConnectionString;
    SqlConnection? _connection;
    SqlCommand? _command;
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

        // Store the EXACT connection string as-is
        // SqlDependency.Start() and new SqlConnection() MUST use the identical string
        _normalizedConnectionString = connectionString;

        // Start SqlDependency
        SqlDependency.Start(_normalizedConnectionString);

        // Give SqlDependency time to initialize Service Broker infrastructure
        await Task.Delay(100, cancellationToken);

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
            SqlDependency.Stop(_normalizedConnectionString ?? connectionString);
        }
        catch
        {
            // Ignore errors during cleanup
        }

        if (_command is not null)
        {
            try
            {
                await _command.DisposeAsync();
            }
            catch
            {
                // Ignore errors during cleanup
            }
            finally
            {
                _command = null;
            }
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

            // Clean up previous dependency and command
            if (_dependency is not null)
            {
                _dependency.OnChange -= OnDependencyChange;
                _dependency = null;
            }

            if (_command is not null)
            {
                try
                {
                    await _command.DisposeAsync();
                }
                catch
                {
                    // Ignore cleanup errors
                }
                _command = null;
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

                _connection = new SqlConnection(_normalizedConnectionString ?? connectionString);
                await _connection.OpenAsync(effectiveCancellationToken);

                // Set the required SET options for SqlDependency on the CONNECTION
                // These are REQUIRED by SQL Server Query Notifications and must be set BEFORE creating the SqlCommand
                const string setOptions = "SET ARITHABORT ON; " +
                                          "SET CONCAT_NULL_YIELDS_NULL ON; " +
                                          "SET QUOTED_IDENTIFIER ON; " +
                                          "SET ANSI_NULLS ON; " +
                                          "SET ANSI_PADDING ON; " +
                                          "SET ANSI_WARNINGS ON; " +
                                          "SET NUMERIC_ROUNDABORT OFF;";

                await using (var setCommand = new SqlCommand(setOptions, _connection))
                {
                    await setCommand.ExecuteNonQueryAsync(effectiveCancellationToken);
                }
            }

            // SqlDependency requires a specific query format
            // Must use two-part name (schema.table) and specific columns (no *)
            // We select all columns to ensure notifications are received for any column change
            // Adding WHERE 1=1 helps ensure SqlDependency can track changes properly
            var sql = $"SELECT {_columnList} FROM dbo.[{_tableName}] WHERE 1=1";

            // IMPORTANT: Do NOT use 'await using' - the command must stay alive for SqlDependency!
            // SqlDependency holds a weak reference and needs the command to exist
            _command = new SqlCommand(sql, _connection);
            _command.CommandTimeout = 30;

            logger.SqlServerSettingUpDependency(_tableName ?? "unknown", sql);

            _dependency = new SqlDependency(_command);
            _dependency.OnChange += OnDependencyChange;

            logger.SqlServerDependencyCreated(_dependency.Id, _dependency.HasChanges);

            // Execute the query to register the dependency
            // Do NOT use 'await using' - it will dispose the connection!
            // SqlDependency requires the connection to stay open to receive notifications
#pragma warning disable RCS1261 // Resource can be disposed asynchronously
#pragma warning disable MA0042 // Prefer using 'await using'
            var reader = await _command.ExecuteReaderAsync(effectiveCancellationToken);
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
        try
        {
            // ALWAYS log all notifications for debugging
            logger.ReceivedSqlServerNotification(e.Type.ToString(), e.Info.ToString(), e.Source.ToString());
            logger.LogInformation($">>> NOTIFICATION RECEIVED: Type={e.Type}, Info={e.Info}, Source={e.Source} <<<");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error logging notification");
        }

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
        // SqlDependency returns Unknown/Truncate most often for data changes, not specific Insert/Update/Delete
        // So we use permissive filtering: exclude only error/config notifications, accept everything else
        var isDataChange = e.Type == SqlNotificationType.Change &&
                          e.Info != SqlNotificationInfo.Error &&
                          e.Info != SqlNotificationInfo.Invalid &&
                          e.Info != SqlNotificationInfo.Options &&
                          e.Info != SqlNotificationInfo.Isolation &&
                          e.Info != SqlNotificationInfo.Query &&
                          e.Info != SqlNotificationInfo.Resource &&
                          e.Info != SqlNotificationInfo.Restart &&
                          e.Info != SqlNotificationInfo.TemplateLimit &&
                          e.Info != SqlNotificationInfo.Merge;

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
