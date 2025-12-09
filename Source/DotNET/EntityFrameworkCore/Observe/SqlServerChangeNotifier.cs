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
    SqlConnection? _connection;
    SqlDependency? _dependency;
    string? _tableName;
    Action? _onChanged;
    CancellationToken _cancellationToken;
    bool _disposed;

    /// <inheritdoc/>
    public async Task StartListeningAsync(string tableName, Action onChanged, CancellationToken cancellationToken = default)
    {
        _tableName = tableName;
        _onChanged = onChanged;
        _cancellationToken = cancellationToken;

        // Start SqlDependency
        SqlDependency.Start(connectionString);

        await SetupDependencyAsync();

        logger.StartedListeningSqlServer(tableName);
    }

    /// <inheritdoc/>
    public async Task StopListeningAsync()
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
        await StopListeningAsync();
    }

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities - table name comes from EF Core metadata
    async Task SetupDependencyAsync()
    {
        if (_cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
        _connection = new SqlConnection(connectionString);
        await _connection.OpenAsync(_cancellationToken);

        // SqlDependency requires a specific query format
        var sql = $"SELECT * FROM dbo.[{_tableName}]";
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
#pragma warning restore CA2100

    void OnDependencyChange(object sender, SqlNotificationEventArgs e)
    {
        logger.ReceivedSqlServerNotification(e.Type.ToString(), e.Info.ToString(), e.Source.ToString());

        if (e.Type == SqlNotificationType.Change)
        {
            _onChanged?.Invoke();
        }

        // Re-subscribe after receiving a notification (SqlDependency is one-shot)
        if (!_cancellationToken.IsCancellationRequested && !_disposed)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await SetupDependencyAsync();
                }
                catch (Exception ex)
                {
                    logger.SqlServerResubscribeError(ex);
                }
            });
        }
    }
}
