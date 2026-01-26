// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore;

/// <summary>
/// Supported database types.
/// </summary>
public enum DatabaseType
{
    /// <summary>
    /// Unknown storage type.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// SQLite storage type.
    /// </summary>
    Sqlite = 1,

    /// <summary>
    /// SQL Server storage type (2022 and earlier).
    /// </summary>
    SqlServer = 2,

    /// <summary>
    /// PostgreSQL storage type.
    /// </summary>
    PostgreSql = 3,

    /// <summary>
    /// SQL Server 2025 storage type with native JSON support.
    /// </summary>
    SqlServer2025 = 4
}
