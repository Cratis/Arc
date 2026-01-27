// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Arc.EntityFrameworkCore;

/// <summary>
/// Extensions for working with database connections.
/// </summary>
public static class DatabaseTypeExtensions
{
    /// <summary>
    /// Infers the database type from the connection string.
    /// </summary>
    /// <param name="connectionString">The connection string to infer the database type from.</param>
    /// <returns>The inferred database type.</returns>
    /// <exception cref="UnsupportedDatabaseType">Thrown if the connection string does not have a supported database type.</exception>
    public static DatabaseType GetDatabaseType(this string connectionString) => connectionString switch
    {
        // SQL Server: Check for SQL Server-specific keywords first since "Data Source=" is ambiguous
        _ when IsSqlServerConnectionString(connectionString) => DatabaseType.SqlServer,

        // PostgreSQL: Uses Host= and Database=
        _ when connectionString.Contains("host=", StringComparison.InvariantCultureIgnoreCase) &&
               connectionString.Contains("database=", StringComparison.InvariantCultureIgnoreCase) => DatabaseType.PostgreSql,

        // SQLite: Uses Data Source= or Filename= but without SQL Server-specific keywords
        _ when connectionString.Contains("data source=", StringComparison.InvariantCultureIgnoreCase) ||
               connectionString.Contains("filename=", StringComparison.InvariantCultureIgnoreCase) => DatabaseType.Sqlite,

        _ => throw new UnsupportedDatabaseType(connectionString)
    };

    /// <summary>
    /// Configures the DbContext to use the database specified in the connection string.
    /// The database type is inferred from the connection string.
    /// </summary>
    /// <param name="builder">The DbContext options builder to configure.</param>
    /// <param name="connectionString">The connection string to use.</param>
    /// <returns>The configured DbContext options builder.</returns>
    /// <exception cref="UnsupportedDatabaseType">Thrown if the connection string does not have a supported database type.</exception>
    public static DbContextOptionsBuilder UseDatabaseFromConnectionString(this DbContextOptionsBuilder builder, string connectionString)
    {
        var type = connectionString.GetDatabaseType();
        return type switch
        {
            DatabaseType.Sqlite => builder
                .UseSqlite(connectionString)
                .EnsurePathsExist(connectionString)
                .ReplaceService<IMigrationsSqlGenerator, MigrationsSqlGeneratorForSqlite>(),

            DatabaseType.SqlServer => builder
                .UseSqlServer(connectionString, options => options.EnableRetryOnFailure())
                .ReplaceService<IMigrationsSqlGenerator, MigrationsSqlGeneratorForSqlServer>(),

            DatabaseType.PostgreSql => builder
                .UseNpgsql(connectionString, options => options.EnableRetryOnFailure())
                .ReplaceService<IMigrationsSqlGenerator, MigrationsSqlGeneratorForPostgreSQL>(),

            _ => throw new UnsupportedDatabaseType(connectionString)
        };
    }

    /// <summary>
    /// Gets the database type from the migration builder's active provider.
    /// </summary>
    /// <param name="migrationBuilder">The migration builder to get the database type from.</param>
    /// <returns>The database type.</returns>
    /// <exception cref="UnsupportedDatabaseType">Thrown if the active provider is not supported.</exception>
    public static DatabaseType GetDatabaseType(this MigrationBuilder migrationBuilder) => migrationBuilder.ActiveProvider.GetDatabaseTypeFromProvider();

    /// <summary>
    /// Gets the database type from the database connection.
    /// </summary>
    /// <param name="database">The database connection to get the type from.</param>
    /// <returns>The database type.</returns>
    /// <exception cref="UnsupportedDatabaseType">Thrown if the connection string does not have a supported database type.</exception>
    public static DatabaseType GetDatabaseType(this DatabaseFacade database) => database.ProviderName.GetDatabaseTypeFromProvider();

    /// <summary>
    /// Gets the database type from the provider name.
    /// </summary>
    /// <param name="providerName">The provider name to get the database type from.</param>
    /// <returns>The database type.</returns>
    /// <exception cref="UnsupportedDatabaseType">Thrown if the provider name is not supported.</exception>
    public static DatabaseType GetDatabaseTypeFromProvider(this string? providerName) => providerName switch
    {
        "Microsoft.EntityFrameworkCore.Sqlite" => DatabaseType.Sqlite,
        "Microsoft.EntityFrameworkCore.SqlServer" => DatabaseType.SqlServer,
        "Npgsql.EntityFrameworkCore.PostgreSQL" => DatabaseType.PostgreSql,
        _ => throw new UnsupportedDatabaseType(providerName!)
    };

    /// <summary>
    /// Ensures that any paths specified in the connection string exist.
    /// This is primarily used for Sqlite databases to ensure that the directory for the database file exists.
    /// </summary>
    /// <param name="builder">The DbContext options builder to configure.</param>
    /// <param name="connectionString">The connection string to use.</param>
    /// <returns>The configured DbContext options builder.</returns>
    public static DbContextOptionsBuilder EnsurePathsExist(this DbContextOptionsBuilder builder, string connectionString)
    {
        var databaseType = connectionString.GetDatabaseType();
        if (databaseType == DatabaseType.Sqlite)
        {
            const string dataSourceKey = "Data Source=";
            const string filenameKey = "Filename=";
            var startIndex = connectionString.IndexOf(dataSourceKey, StringComparison.OrdinalIgnoreCase);
            if (startIndex == -1)
            {
                startIndex = connectionString.IndexOf(filenameKey, StringComparison.OrdinalIgnoreCase);
                if (startIndex == -1)
                {
                    return builder;
                }
                startIndex += filenameKey.Length;
            }
            else
            {
                startIndex += dataSourceKey.Length;
            }

            var endIndex = connectionString.IndexOf(';', startIndex);
            var path = endIndex == -1
                ? connectionString[startIndex..]
                : connectionString[startIndex..endIndex];

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        return builder;
    }

    static bool IsSqlServerConnectionString(string connectionString)
    {
        // SQL Server connection strings typically use "Server=" or "Data Source=" with additional SQL Server-specific keywords
        var hasServerKeyword = connectionString.Contains("server=", StringComparison.InvariantCultureIgnoreCase);
        var hasDataSource = connectionString.Contains("data source=", StringComparison.InvariantCultureIgnoreCase);

        if (!hasServerKeyword && !hasDataSource)
        {
            return false;
        }

        // Check for SQL Server-specific keywords that distinguish it from SQLite
        return connectionString.Contains("initial catalog=", StringComparison.InvariantCultureIgnoreCase) ||
               connectionString.Contains("database=", StringComparison.InvariantCultureIgnoreCase) ||
               connectionString.Contains("user id=", StringComparison.InvariantCultureIgnoreCase) ||
               connectionString.Contains("integrated security=", StringComparison.InvariantCultureIgnoreCase) ||
               connectionString.Contains("trust server certificate=", StringComparison.InvariantCultureIgnoreCase) ||
               connectionString.Contains("trustservercertificate=", StringComparison.InvariantCultureIgnoreCase) ||
               connectionString.Contains("encrypt=", StringComparison.InvariantCultureIgnoreCase) ||
               connectionString.Contains("multipleactiveresultsets=", StringComparison.InvariantCultureIgnoreCase) ||
               connectionString.Contains("application name=", StringComparison.InvariantCultureIgnoreCase);
    }
}