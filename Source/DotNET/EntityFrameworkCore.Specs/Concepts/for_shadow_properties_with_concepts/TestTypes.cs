// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_shadow_properties_with_concepts;

#pragma warning disable SA1402, SA1649

public record SampleId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly SampleId NotSet = new(Guid.Empty);
    public static implicit operator SampleId(Guid value) => new(value);
}

public record RelatedId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly RelatedId NotSet = new(Guid.Empty);
    public static implicit operator RelatedId(Guid value) => new(value);
}

public class SampleEntity
{
    public SampleId Id { get; set; } = SampleId.NotSet;
    public string Name { get; set; } = string.Empty;
}

public class RelatedEntity
{
    public RelatedId Id { get; set; } = RelatedId.NotSet;
    public string Description { get; set; } = string.Empty;
}

public class EntityWithShadowProperties
{
    public Guid Id { get; set; }
    public SampleEntity Sample { get; set; } = null!;
    public RelatedEntity Related { get; set; } = null!;
}

public class EntityWithShadowPropertiesConfiguration : IEntityTypeConfiguration<EntityWithShadowProperties>
{
    public void Configure(EntityTypeBuilder<EntityWithShadowProperties> builder)
    {
        builder.ToTable("EntityWithShadowProperties");
        builder.HasKey(e => e.Id);

        // Using shadow properties with ConceptAs types
        builder.Property<SampleId>("SampleId");
        builder.Property<RelatedId>("RelatedId");

        builder.HasOne(e => e.Sample).WithMany().HasForeignKey("SampleId");
        builder.HasOne(e => e.Related).WithMany().HasForeignKey("RelatedId");
    }
}

public class EntityWithConceptPropertiesConfiguredAsShadow
{
    public Guid Id { get; set; }
    public SampleId SampleId { get; set; } = SampleId.NotSet;
    public RelatedId RelatedId { get; set; } = RelatedId.NotSet;
}

public class EntityWithConceptPropertiesConfiguredAsShadowConfiguration : IEntityTypeConfiguration<EntityWithConceptPropertiesConfiguredAsShadow>
{
    public void Configure(EntityTypeBuilder<EntityWithConceptPropertiesConfiguredAsShadow> builder)
    {
        builder.ToTable("EntityWithConceptPropertiesConfiguredAsShadow");

        // This mirrors the user's scenario - using Property<T>(string) syntax on existing properties
        builder.Property<Guid>("Id");
        builder.Property<SampleId>("SampleId");
        builder.Property<RelatedId>("RelatedId");
        builder.HasKey("Id");
    }
}

public class TestDbContextWithShadowProperties(DbContextOptions<TestDbContextWithShadowProperties> options) : DbContext(options)
{
    public DbSet<EntityWithShadowProperties> Entities { get; set; }
    public DbSet<EntityWithConceptPropertiesConfiguredAsShadow> ConceptEntities { get; set; }
    public DbSet<SampleEntity> Samples { get; set; }
    public DbSet<RelatedEntity> Related { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SampleEntity>(builder =>
        {
            builder.ToTable("Samples");
            builder.HasKey(e => e.Id);
        })
            .Entity<RelatedEntity>(builder =>
        {
            builder.ToTable("Related");
            builder.HasKey(e => e.Id);
        })
            .ApplyConfiguration(new EntityWithShadowPropertiesConfiguration())
            .ApplyConfiguration(new EntityWithConceptPropertiesConfiguredAsShadowConfiguration());
    }
}

#pragma warning restore SA1402, SA1649
