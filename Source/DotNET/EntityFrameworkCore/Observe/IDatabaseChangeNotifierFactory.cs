// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore.Observe;

/// <summary>
/// Defines a factory for creating <see cref="IDatabaseChangeNotifier"/> instances.
/// </summary>
public interface IDatabaseChangeNotifierFactory
{
    /// <summary>
    /// Creates a <see cref="IDatabaseChangeNotifier"/> for the specified database type and connection string.
    /// </summary>
    /// <param name="databaseType">The type of database.</param>
    /// <param name="connectionString">The connection string to the database.</param>
    /// <returns>A database change notifier appropriate for the database type.</returns>
    IDatabaseChangeNotifier Create(DatabaseType databaseType, string connectionString);
}
