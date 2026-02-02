// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore;

/// <summary>
/// Represents the configuration options for Entity Framework Core.
/// </summary>
public class EntityFrameworkCoreOptions
{
    /// <summary>
    /// Gets or sets the connection string for the database.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to automatically discover and register DbContext types
    /// that inherit from <see cref="BaseDbContext"/> or <see cref="ReadOnlyDbContext"/>.
    /// Defaults to true.
    /// </summary>
    public bool AutoDiscoverDbContexts { get; set; } = true;
}
