// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Arc.Connections;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Cratis.Arc.EntityFrameworkCore.Connections;

/// <summary>
/// Provides extension methods for applying a <see cref="ResolvedConnection"/> to a <see cref="DbConnection"/>.
/// </summary>
public static class ResolvedConnectionDbConnectionExtensions
{
    /// <summary>
    /// Get the storage kind of a connection, one of <see cref="WellKnownConnectionStorage"/>.
    /// </summary>
    /// <param name="connection">The <see cref="DbConnection"/>.</param>
    /// <returns>The storage kind.</returns>
    /// <exception cref="UnsupportedDatabaseType">Thrown when the connection is not for a supported database.</exception>
    public static string GetConnectionStorage(this DbConnection connection) => connection switch
    {
        SqlConnection => WellKnownConnectionStorage.SqlServer,
        NpgsqlConnection => WellKnownConnectionStorage.PostgreSql,
        SqliteConnection => WellKnownConnectionStorage.Sqlite,
        _ => throw new UnsupportedDatabaseType(connection.GetType().FullName ?? connection.GetType().Name)
    };

    /// <summary>
    /// Apply a <see cref="ResolvedConnection"/> over a configured connection string, onto a closed connection.
    /// </summary>
    /// <param name="connection">The <see cref="DbConnection"/> to apply to. It must not be open.</param>
    /// <param name="configured">The configured connection string the resolution applies over.</param>
    /// <param name="resolved">The <see cref="ResolvedConnection"/>.</param>
    /// <remarks>
    /// <list type="bullet">
    /// <item>A resolved connection string replaces the configured one.</item>
    /// <item>A resolved database replaces the database of the connection string — the file for SQLite.</item>
    /// <item>A user name and password replace those of the connection string. SQLite has no identity, so it is ignored.</item>
    /// <item>An access token is set as <see cref="SqlConnection.AccessToken"/> for SQL Server and as the password for PostgreSQL.</item>
    /// </list>
    /// </remarks>
    public static void ApplyResolvedConnection(this DbConnection connection, string configured, ResolvedConnection resolved)
    {
        var connectionString = string.IsNullOrEmpty(resolved.ConnectionString) ? configured : resolved.ConnectionString;
        switch (connection)
        {
            case SqlConnection sql:
                ApplyTo(sql, connectionString, resolved);
                break;

            case NpgsqlConnection npgsql:
                ApplyTo(npgsql, connectionString, resolved);
                break;

            default:
                ApplyTo(connection, connectionString, resolved);
                break;
        }
    }

    static void ApplyTo(SqlConnection connection, string connectionString, ResolvedConnection resolved)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (resolved.Database is not null)
        {
            builder.InitialCatalog = resolved.Database;
        }

        if (resolved.Identity is { UserName: { } userName })
        {
            builder.UserID = userName;
        }

        if (resolved.Identity is { Password: { } password })
        {
            builder.Password = password;
        }

        // The access token must be cleared before the connection string changes, and set after: SQL Server refuses an
        // access token next to a password or integrated security, in either order of assignment.
        connection.AccessToken = null;
        connection.ConnectionString = builder.ConnectionString;
        connection.AccessToken = resolved.Identity?.AccessToken;
    }

    static void ApplyTo(NpgsqlConnection connection, string connectionString, ResolvedConnection resolved)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (resolved.Database is not null)
        {
            builder.Database = resolved.Database;
        }

        if (resolved.Identity is { UserName: { } userName })
        {
            builder.Username = userName;
        }

        if ((resolved.Identity?.Password ?? resolved.Identity?.AccessToken) is { } password)
        {
            builder.Password = password;
        }

        connection.ConnectionString = builder.ConnectionString;
    }

    static void ApplyTo(DbConnection connection, string connectionString, ResolvedConnection resolved)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (resolved.Database is not null)
        {
            builder.DataSource = resolved.Database;
        }

        connection.ConnectionString = builder.ConnectionString;
    }
}
