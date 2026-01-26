// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;

namespace Cratis.Arc.EntityFrameworkCore;

/// <summary>
/// Provides SQL Server version detection capabilities.
/// </summary>
public static class SqlServerVersionDetector
{
    /// <summary>
    /// SQL Server 2025 major version number.
    /// </summary>
    public const int SqlServer2025MajorVersion = 17;

    static readonly ConcurrentDictionary<string, int> _serverVersionCache = new();
    static int? _overrideMajorVersion;

    /// <summary>
    /// Determines if the SQL Server instance supports native JSON type (SQL Server 2025+).
    /// If an override is set, uses the override. Otherwise queries the database.
    /// </summary>
    /// <param name="connectionString">The connection string to the SQL Server instance.</param>
    /// <returns>True if the server is SQL Server 2025 or later; otherwise false.</returns>
    public static bool IsSqlServer2025OrLater(string connectionString)
    {
        if (_overrideMajorVersion.HasValue)
        {
            return _overrideMajorVersion.Value >= SqlServer2025MajorVersion;
        }

        var majorVersion = GetMajorVersion(connectionString);
        return majorVersion >= SqlServer2025MajorVersion;
    }

    /// <summary>
    /// Determines if SQL Server 2025 or later should be assumed based on cached information.
    /// Used when connection string is not available (e.g., during migration generation from provider name only).
    /// </summary>
    /// <returns>True if override is set to 2025+ or if any cached server is 2025+; otherwise false.</returns>
    public static bool ShouldAssumeServer2025()
    {
        if (_overrideMajorVersion.HasValue)
        {
            return _overrideMajorVersion.Value >= SqlServer2025MajorVersion;
        }

        // Check if any cached SQL Server is 2025+
        foreach (var kvp in _serverVersionCache)
        {
            if (kvp.Value >= SqlServer2025MajorVersion)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the major version number of the SQL Server instance.
    /// Results are cached by server name.
    /// </summary>
    /// <param name="connectionString">The connection string to the SQL Server instance.</param>
    /// <returns>The major version number of the SQL Server instance.</returns>
    public static int GetMajorVersion(string connectionString)
    {
        if (_overrideMajorVersion.HasValue)
        {
            return _overrideMajorVersion.Value;
        }

        var serverName = ExtractServerName(connectionString);
        return _serverVersionCache.GetOrAdd(serverName, static (_, connStr) => QueryMajorVersion(connStr), connectionString);
    }

    /// <summary>
    /// Sets an override for the SQL Server major version.
    /// Use this for testing or when targeting a specific version at design-time.
    /// </summary>
    /// <param name="majorVersion">The major version to assume, or null to clear the override.</param>
    public static void SetVersionOverride(int? majorVersion) => _overrideMajorVersion = majorVersion;

    /// <summary>
    /// Sets the override to assume SQL Server 2025.
    /// Use this for testing or when targeting SQL Server 2025 at design-time.
    /// </summary>
    public static void AssumeServer2025() => _overrideMajorVersion = SqlServer2025MajorVersion;

    /// <summary>
    /// Clears all cached data and overrides.
    /// </summary>
    public static void Reset()
    {
        _serverVersionCache.Clear();
        _overrideMajorVersion = null;
    }

    /// <summary>
    /// Clears the version cache. Useful for testing.
    /// </summary>
    public static void ClearCache() => _serverVersionCache.Clear();

    static string ExtractServerName(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        return builder.DataSource.ToLowerInvariant();
    }

    static int QueryMajorVersion(string connectionString)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT SERVERPROPERTY('ProductMajorVersion')";
            var result = command.ExecuteScalar();

            return result is not null && int.TryParse(result.ToString(), out var version)
                ? version
                : 0;
        }
        catch
        {
            // If we can't connect, default to SQL Server 2022 behavior (nvarchar(max) for JSON)
            return 0;
        }
    }
}
