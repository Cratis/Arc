// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.for_PropertyExtensions;

public class PlaceDbContext(DbContextOptions<PlaceDbContext> options) : DbContext(options)
{
    public DbSet<Place> Places => Set<Place>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Place>()
            .Property(_ => _.Location)
            .AsCoordinate(DatabaseType.Sqlite);
}
