// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cratis.Arc.EntityFrameworkCore.for_ReadOnlyDbContextExtensions;

public class TestReadOnlyEntityConfiguration : IEntityTypeConfiguration<TestReadOnlyEntity>
{
    public void Configure(EntityTypeBuilder<TestReadOnlyEntity> builder)
    {
        builder.ToTable("TestReadOnlyEntities");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasConversion(
            v => v.Value,
            v => new TestReadOnlyId(v));
        builder.Property(e => e.Name).IsRequired();
    }
}
