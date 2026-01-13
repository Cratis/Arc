// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_querying_with_concepts;

#pragma warning disable SA1402, SA1649

public record MissionId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly MissionId NotSet = new(Guid.Empty);
    public static implicit operator MissionId(Guid value) => new(value);
    public static MissionId New() => new(Guid.NewGuid());
}

public record ResourceId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly ResourceId NotSet = new(Guid.Empty);
    public static implicit operator ResourceId(Guid value) => new(value);
    public static ResourceId New() => new(Guid.NewGuid());
}

public class ResponsePhase
{
    public MissionId Id { get; set; } = MissionId.NotSet;
    public ResourceId ResourceId { get; set; } = ResourceId.NotSet;
    public string? Name { get; set; }
}

/// <summary>
/// DbContext without manual value conversions - relies on ConceptAsModelCustomizer.
/// </summary>
/// <param name="options">The DbContext options.</param>
public class ResponsePhaseDbContext(DbContextOptions<ResponsePhaseDbContext> options) : DbContext(options)
{
    public DbSet<ResponsePhase> ResponsePhases => Set<ResponsePhase>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Ignore ConceptAs types from being treated as entities
        modelBuilder.Ignore<MissionId>();
        modelBuilder.Ignore<ResourceId>();

        modelBuilder.Entity<ResponsePhase>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Explicitly configure the properties to ensure they're discovered
            entity.Property(e => e.Id);
            entity.Property(e => e.ResourceId);
        });
    }
}
