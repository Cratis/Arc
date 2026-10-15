// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// Holds the names of the storage kinds a <see cref="ConnectionRequest"/> can ask for.
/// </summary>
public static class WellKnownConnectionStorage
{
    /// <summary>
    /// MongoDB.
    /// </summary>
    public const string MongoDB = "MongoDB";

    /// <summary>
    /// Microsoft SQL Server, including Azure SQL.
    /// </summary>
    public const string SqlServer = "SqlServer";

    /// <summary>
    /// PostgreSQL.
    /// </summary>
    public const string PostgreSql = "PostgreSql";

    /// <summary>
    /// SQLite.
    /// </summary>
    public const string Sqlite = "Sqlite";
}
