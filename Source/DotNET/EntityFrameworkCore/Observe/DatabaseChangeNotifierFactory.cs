// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.EntityFrameworkCore.Observe;

/// <summary>
/// Factory for creating <see cref="IDatabaseChangeNotifier"/> instances based on database type.
/// </summary>
/// <param name="loggerFactory">The logger factory.</param>
public class DatabaseChangeNotifierFactory(ILoggerFactory loggerFactory) : IDatabaseChangeNotifierFactory
{
    /// <inheritdoc/>
    public IDatabaseChangeNotifier Create(DbContext dbContext)
    {
        var databaseType = dbContext.Database.GetDatabaseType();
        var connectionString = dbContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Connection string is not available from the DbContext.");

        return databaseType switch
        {
            DatabaseType.PostgreSql => new PostgreSqlChangeNotifier(
                connectionString,
                loggerFactory.CreateLogger<PostgreSqlChangeNotifier>()),

            DatabaseType.SqlServer => new SqlServerChangeNotifier(
                connectionString,
                loggerFactory.CreateLogger<SqlServerChangeNotifier>()),

            DatabaseType.Sqlite => new SqliteChangeNotifier(
                connectionString,
                loggerFactory.CreateLogger<SqliteChangeNotifier>()),

            _ => throw new UnsupportedDatabaseType(databaseType.ToString())
        };
    }
}
