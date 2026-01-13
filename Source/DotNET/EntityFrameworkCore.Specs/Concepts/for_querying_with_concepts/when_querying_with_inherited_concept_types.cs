// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_querying_with_concepts;

#pragma warning disable SA1402, SA1649

/// <summary>
/// Test that queries with concept types that inherit from other concept types.
/// This mimics the scenario where MissionId inherits from EventSourceId which inherits from ConceptAs{string}.
/// </summary>
[Collection(nameof(ConceptAsQueryingCollection))]
public class when_querying_with_inherited_concept_types : Specification
{
    InheritedConceptDbContext _context = null!;
    SqliteConnection _connection = null!;
    DerivedMissionId _missionId;
    DerivedResourceId _resourceId;
    InheritedConceptEntity? _result;

    async Task Establish()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<InheritedConceptDbContext>()
            .UseSqlite(_connection)
            .AddConceptAsSupport()
            .Options;

        _context = new InheritedConceptDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _missionId = DerivedMissionId.New();
        _resourceId = DerivedResourceId.New();
        var entity = new InheritedConceptEntity
        {
            Id = _missionId,
            ResourceId = _resourceId,
            Name = "Test Entity"
        };
        await _context.Entities.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    void Because() => _result = _context.Entities.SingleOrDefault(e => e.Id == _missionId && e.ResourceId == _resourceId);

    async Task Destroy()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact] void should_find_the_entity() => _result.ShouldNotBeNull();
    [Fact] void should_have_correct_mission_id() => _result.Id.Value.ShouldEqual(_missionId.Value);
    [Fact] void should_have_correct_resource_id() => _result.ResourceId.Value.ShouldEqual(_resourceId.Value);
    [Fact] void should_have_correct_name() => _result.Name.ShouldEqual("Test Entity");
}

/// <summary>
/// Base concept type that mimics Chronicle's EventSourceId.
/// </summary>
/// <param name="Value">The string value.</param>
public record EventSourceIdBase(string Value) : ConceptAs<string>(Value);

/// <summary>
/// Derived concept type that mimics user's MissionId.
/// </summary>
/// <param name="Value">The string value.</param>
public record DerivedMissionId(string Value) : EventSourceIdBase(Value)
{
    public static readonly DerivedMissionId NotSet = new("[not-set]");
    public static implicit operator DerivedMissionId(string value) => new(value);
    public static DerivedMissionId New() => new(Guid.NewGuid().ToString());
}

/// <summary>
/// ResourceId as int concept - mimics user's ResourceId.
/// </summary>
/// <param name="Value">The int value.</param>
public record DerivedResourceId(int Value) : ConceptAs<int>(Value)
{
    public static readonly DerivedResourceId NotSet = new(0);
    public static implicit operator DerivedResourceId(int value) => new(value);
    public static DerivedResourceId New() => new(Random.Shared.Next(1, 100000));
}

/// <summary>
/// Entity with inherited concept types.
/// </summary>
public class InheritedConceptEntity
{
    public DerivedMissionId Id { get; set; } = DerivedMissionId.NotSet;
    public DerivedResourceId ResourceId { get; set; } = DerivedResourceId.NotSet;
    public string? Name { get; set; }
}

/// <summary>
/// DbContext for inherited concept tests.
/// </summary>
/// <param name="options">The DbContext options.</param>
public class InheritedConceptDbContext(DbContextOptions<InheritedConceptDbContext> options) : DbContext(options)
{
    public DbSet<InheritedConceptEntity> Entities => Set<InheritedConceptEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<DerivedMissionId>()
            .Ignore<DerivedResourceId>()
            .Ignore<EventSourceIdBase>()
            .Entity<InheritedConceptEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id);
                entity.Property(e => e.ResourceId);
            });
    }
}
