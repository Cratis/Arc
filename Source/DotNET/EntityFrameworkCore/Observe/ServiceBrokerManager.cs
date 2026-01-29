// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.EntityFrameworkCore.Observe;

/// <summary>
/// Manages Service Broker enablement for SQL Server databases.
/// </summary>
/// <param name="logger">The logger.</param>
public class ServiceBrokerManager(ILogger<ServiceBrokerManager> logger) : IServiceBrokerManager
{
    /// <summary>
    /// Track databases where we've already attempted to enable Service Broker (per server+database).
    /// </summary>
    static readonly HashSet<string> _enablementAttempted = [];
    static readonly object _lock = new();

    /// <inheritdoc/>
    public async Task EnsureEnabled(string connectionString, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            // Get database name and server for tracking
            string databaseKey;
            await using (var dbNameCommand = new SqlCommand("SELECT @@SERVERNAME + '|' + DB_NAME()", connection))
            {
                databaseKey = (string)(await dbNameCommand.ExecuteScalarAsync(cancellationToken))!;
            }

            // Check if Service Broker is enabled
            await using var checkCommand = new SqlCommand(
                "SELECT is_broker_enabled FROM sys.databases WHERE database_id = DB_ID()",
                connection);
            var result = await checkCommand.ExecuteScalarAsync(cancellationToken);

            if (result is bool isEnabled)
            {
                if (isEnabled)
                {
                    logger.ServiceBrokerEnabled();
                    return;
                }

                // Service Broker is disabled - attempt to enable it once per database
                bool shouldAttemptEnable;
                lock (_lock)
                {
                    shouldAttemptEnable = _enablementAttempted.Add(databaseKey);
                }

                if (shouldAttemptEnable)
                {
                    logger.AttemptingToEnableServiceBroker(databaseKey);
                    await TryEnableServiceBroker(connection, databaseKey, cancellationToken);
                }
                else
                {
                    logger.ServiceBrokerDisabled();
                }
            }
        }
        catch (Exception ex)
        {
            logger.ServiceBrokerCheckFailed(ex);
        }
    }

    async Task TryEnableServiceBroker(SqlConnection connection, string databaseKey, CancellationToken cancellationToken)
    {
        try
        {
            // Get the database name
            await using var dbNameCommand = new SqlCommand("SELECT DB_NAME()", connection);
            var dbName = (string)(await dbNameCommand.ExecuteScalarAsync(cancellationToken))!;

            // Try to enable Service Broker - database name comes from DB_NAME() system function
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            var enableSql = $"ALTER DATABASE [{dbName}] SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE";
            await using var enableCommand = new SqlCommand(enableSql, connection)
            {
                CommandTimeout = 60
            };
#pragma warning restore CA2100

            await enableCommand.ExecuteNonQueryAsync(cancellationToken);

            // Verify it worked
            await using var verifyCommand = new SqlCommand(
                "SELECT is_broker_enabled FROM sys.databases WHERE database_id = DB_ID()",
                connection);
            var verifyResult = await verifyCommand.ExecuteScalarAsync(cancellationToken);

            if (verifyResult is bool isNowEnabled && isNowEnabled)
            {
                logger.ServiceBrokerEnabledSuccessfully(databaseKey);
            }
            else
            {
                logger.ServiceBrokerEnablementFailed(databaseKey);
            }
        }
        catch (SqlException ex) when (ex.Number == 5011)
        {
            // Database in use
            logger.ServiceBrokerEnablementFailedDatabaseInUse(databaseKey, ex);
        }
        catch (SqlException ex) when (ex.Number == 229 || ex.Number == 262)
        {
            // Permission denied
            logger.ServiceBrokerEnablementFailedPermissions(databaseKey, ex);
        }
        catch (Exception ex)
        {
            logger.ServiceBrokerEnablementFailedUnexpected(databaseKey, ex);
        }
    }
}
