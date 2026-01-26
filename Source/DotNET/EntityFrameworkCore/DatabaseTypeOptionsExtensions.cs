// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Cratis.Arc.EntityFrameworkCore;

/// <summary>
/// Extension methods for configuring the database type in DbContext options.
/// </summary>
public static class DatabaseTypeOptionsExtensions
{
    /// <summary>
    /// Stores the database type in the DbContext options for later retrieval.
    /// </summary>
    /// <param name="builder">The DbContext options builder to configure.</param>
    /// <param name="databaseType">The database type to store.</param>
    /// <returns>The configured DbContext options builder.</returns>
    public static DbContextOptionsBuilder WithDatabaseType(this DbContextOptionsBuilder builder, DatabaseType databaseType)
    {
        ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(
            new DatabaseTypeOptionsExtension(databaseType));
        return builder;
    }

    /// <summary>
    /// Gets the configured database type from the DbContext, falling back to provider detection.
    /// </summary>
    /// <param name="context">The DbContext to get the database type from.</param>
    /// <returns>The configured database type.</returns>
    public static DatabaseType GetConfiguredDatabaseType(this DbContext context)
    {
        var extension = context.GetService<IDbContextOptions>()
            .FindExtension<DatabaseTypeOptionsExtension>();

        return extension?.DatabaseType ?? context.Database.GetDatabaseType();
    }
}
