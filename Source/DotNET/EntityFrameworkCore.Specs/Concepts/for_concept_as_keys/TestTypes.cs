// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_concept_as_keys;

#pragma warning disable SA1402, SA1649

public record EntityId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly EntityId NotSet = new(Guid.Empty);
    public static implicit operator EntityId(Guid value) => new(value);
}

public class EntityWithConceptKey
{
    public EntityId Id { get; set; } = EntityId.NotSet;
    public string Name { get; set; } = string.Empty;
}

public class EntityWithConceptKeyConfiguration : IEntityTypeConfiguration<EntityWithConceptKey>
{
    public void Configure(EntityTypeBuilder<EntityWithConceptKey> builder)
    {
        builder.ToTable("EntityWithConceptKey");
        builder.Property(e => e.Id);
        builder.HasKey(e => e.Id);
    }
}

public class TestDbContextWithConceptKey(DbContextOptions<TestDbContextWithConceptKey> options) : BaseDbContext(options)
{
    public DbSet<EntityWithConceptKey> Entities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new EntityWithConceptKeyConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}

#pragma warning restore SA1402, SA1649
