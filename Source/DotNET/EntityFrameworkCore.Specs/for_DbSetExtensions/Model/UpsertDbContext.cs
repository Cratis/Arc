// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.for_DbSetExtensions.Model;

public class UpsertDbContext(DbContextOptions<UpsertDbContext> options) : Microsoft.EntityFrameworkCore.DbContext(options)
{
    public DbSet<UpsertItem> Items => Set<UpsertItem>();
    public DbSet<KeyOnlyItem> Keys => Set<KeyOnlyItem>();
    public DbSet<GeneratedKeyItem> Generated => Set<GeneratedKeyItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var item = modelBuilder.Entity<UpsertItem>();
        item.ToTable("Order", "upsert-tests");
        item.HasKey(_ => new { _.Tenant, _.Id });
        item.Property(_ => _.Id).ValueGeneratedNever();
        item.Property(_ => _.Tenant).HasColumnName("group");
        item.Property(_ => _.Value).HasColumnName("select").HasConversion(_ => _.Value, _ => new UpsertValue(_));
        modelBuilder.Entity<KeyOnlyItem>().ToTable("Key", "upsert-tests");
        modelBuilder.Entity<GeneratedKeyItem>().ToTable("Generated", "upsert-tests");
    }
}
