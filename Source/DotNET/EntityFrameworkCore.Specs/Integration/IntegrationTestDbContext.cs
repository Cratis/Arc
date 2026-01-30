// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Integration;

/// <summary>
/// DbContext used for integration testing WebSocket observable queries.
/// </summary>
/// <param name="options">The DbContext options.</param>
public class IntegrationTestDbContext(DbContextOptions<IntegrationTestDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets or sets the test entities DbSet.
    /// </summary>
    public DbSet<IntegrationTestEntity> Entities { get; set; }
}
