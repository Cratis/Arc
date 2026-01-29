// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.EntityFrameworkCore.Observe;

/// <summary>
/// Factory for creating <see cref="IDatabaseChangeNotifier"/> instances based on database type.
/// </summary>
/// <param name="loggerFactory">The logger factory.</param>
/// <param name="serviceBrokerManager">The Service Broker manager for SQL Server.</param>
public class DatabaseChangeNotifierFactory(ILoggerFactory loggerFactory, IServiceBrokerManager serviceBrokerManager) : IDatabaseChangeNotifierFactory
{
    /// <inheritdoc/>
    public IDatabaseChangeNotifier Create(DatabaseType databaseType, string connectionString)
    {
        return databaseType switch
        {
            DatabaseType.PostgreSql => new PostgreSqlChangeNotifier(
                connectionString,
                loggerFactory.CreateLogger<PostgreSqlChangeNotifier>()),

            DatabaseType.SqlServer => new SqlServerChangeNotifier(
                connectionString,
                serviceBrokerManager,
                loggerFactory.CreateLogger<SqlServerChangeNotifier>()),

            DatabaseType.Sqlite => new SqliteChangeNotifier(
                connectionString,
                loggerFactory.CreateLogger<SqliteChangeNotifier>()),

            _ => throw new UnsupportedDatabaseType(databaseType.ToString())
        };
    }
}
