// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cratis.Arc.EntityFrameworkCore.for_DbContextServiceCollectionExtensions;

public class TestEntityConfiguration : IEntityTypeConfiguration<TestEntity>
{
    public void Configure(EntityTypeBuilder<TestEntity> builder)
    {
        builder.ToTable("TestEntities");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasConversion(
            v => v.Value,
            v => new TestId(v));
        builder.Property(e => e.Name).IsRequired();
    }
}
