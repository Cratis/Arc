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
    /// For SQL Server, this will connect to the database to detect the version.
    /// </summary>
    /// <param name="connectionString">The connection string to infer the database type from.</param>
    /// <returns>The inferred database type.</returns>
    /// <exception cref="UnsupportedDatabaseType">Thrown if the connection string does not have a supported database type.</exception>
    public static DatabaseType GetDatabaseType(this string connectionString) => connectionString switch
    {
        _ when connectionString.Contains("data source=", StringComparison.InvariantCultureIgnoreCase) ||
               connectionString.Contains("filename=", StringComparison.InvariantCultureIgnoreCase) => DatabaseType.Sqlite,

        _ when connectionString.Contains("server=", StringComparison.InvariantCultureIgnoreCase) &&
               connectionString.Contains("database=", StringComparison.InvariantCultureIgnoreCase) =>
               SqlServerVersionDetector.IsSqlServer2025OrLater(connectionString) ? DatabaseType.SqlServer2025 : DatabaseType.SqlServer,

        _ when connectionString.Contains("host=", StringComparison.InvariantCultureIgnoreCase) &&
               connectionString.Contains("database=", StringComparison.InvariantCultureIgnoreCase) => DatabaseType.PostgreSql,
        _ => throw new UnsupportedDatabaseType(connectionString)
    };

    /// <summary>
    /// Configures the DbContext to use the database specified in the connection string.
    /// The database type is inferred from the connection string by connecting to detect the version.
    /// </summary>
    /// <param name="builder">The DbContext options builder to configure.</param>
    /// <param name="connectionString">The connection string to use.</param>
    /// <returns>The configured DbContext options builder.</returns>
    /// <exception cref="UnsupportedDatabaseType">Thrown if the connection string does not have a supported database type.</exception>
    public static DbContextOptionsBuilder UseDatabaseFromConnectionString(this DbContextOptionsBuilder builder, string connectionString)
    {
        var databaseType = connectionString.GetDatabaseType();
        builder.WithDatabaseType(databaseType);
        return databaseType switch
        {
            DatabaseType.Sqlite => builder
                .UseSqlite(connectionString)
                .EnsurePathsExist(connectionString)
                .ReplaceService<IMigrationsSqlGenerator, MigrationsSqlGeneratorForSqlite>(),

            DatabaseType.SqlServer or DatabaseType.SqlServer2025 => builder
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
    /// For SQL Server, returns the cached version if available, otherwise defaults to SQL Server 2022.
    /// </summary>
    /// <param name="migrationBuilder">The migration builder to get the database type from.</param>
    /// <returns>The database type.</returns>
    /// <exception cref="UnsupportedDatabaseType">Thrown if the active provider is not supported.</exception>
    public static DatabaseType GetDatabaseType(this MigrationBuilder migrationBuilder) => migrationBuilder.ActiveProvider.GetDatabaseTypeFromProvider();

    /// <summary>
    /// Gets the database type from the database connection.
    /// For SQL Server, attempts to detect the version from the connection.
    /// </summary>
    /// <param name="database">The database connection to get the type from.</param>
    /// <returns>The database type.</returns>
    /// <exception cref="UnsupportedDatabaseType">Thrown if the connection string does not have a supported database type.</exception>
    public static DatabaseType GetDatabaseType(this DatabaseFacade database)
    {
        var providerName = database.ProviderName;
        if (providerName == "Microsoft.EntityFrameworkCore.SqlServer")
        {
            var connectionString = database.GetConnectionString();
            if (!string.IsNullOrEmpty(connectionString))
            {
                return SqlServerVersionDetector.IsSqlServer2025OrLater(connectionString)
                    ? DatabaseType.SqlServer2025
                    : DatabaseType.SqlServer;
            }
        }

        return providerName.GetDatabaseTypeFromProvider();
    }

    /// <summary>
    /// Gets the database type from the provider name.
    /// For SQL Server, uses cached version info or override if available.
    /// </summary>
    /// <param name="providerName">The provider name to get the database type from.</param>
    /// <returns>The database type.</returns>
    /// <exception cref="UnsupportedDatabaseType">Thrown if the provider name is not supported.</exception>
    public static DatabaseType GetDatabaseTypeFromProvider(this string? providerName) => providerName switch
    {
        "Microsoft.EntityFrameworkCore.Sqlite" => DatabaseType.Sqlite,
        "Microsoft.EntityFrameworkCore.SqlServer" => SqlServerVersionDetector.ShouldAssumeServer2025() ? DatabaseType.SqlServer2025 : DatabaseType.SqlServer,
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
}