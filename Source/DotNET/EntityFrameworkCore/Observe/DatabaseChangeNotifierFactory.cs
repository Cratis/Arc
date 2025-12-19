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
                ExtractSqlitePath(connectionString),
                loggerFactory.CreateLogger<SqliteChangeNotifier>()),

            _ => throw new UnsupportedDatabaseType(databaseType.ToString())
        };
    }

    static string ExtractSqlitePath(string connectionString)
    {
        const string dataSourceKey = "Data Source=";
        const string filenameKey = "Filename=";

        var startIndex = connectionString.IndexOf(dataSourceKey, StringComparison.OrdinalIgnoreCase);
        if (startIndex == -1)
        {
            startIndex = connectionString.IndexOf(filenameKey, StringComparison.OrdinalIgnoreCase);
            if (startIndex == -1)
            {
                return connectionString;
            }
            startIndex += filenameKey.Length;
        }
        else
        {
            startIndex += dataSourceKey.Length;
        }

        var endIndex = connectionString.IndexOf(';', startIndex);
        return endIndex == -1
            ? connectionString[startIndex..]
            : connectionString[startIndex..endIndex];
    }
}
