// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_querying_with_concepts;

#pragma warning disable SA1402, SA1649

/// <summary>
/// Tests the exact pattern from user's code:
/// - Concept types passed as method parameters
/// - Used in a lambda expression for filtering.
/// </summary>
[Collection(nameof(ConceptAsQueryingCollection))]
public class when_querying_with_method_parameter_concepts : Specification
{
    TestQueryDbContext _context = null!;
    SqliteConnection _connection = null!;
    MethodParamMissionId _missionId;
    MethodParamResourceId _resourceId;
    MethodParamResponsePhase? _result;

    async Task Establish()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TestQueryDbContext>()
            .UseSqlite(_connection)
            .AddConceptAsSupport()
            .Options;

        _context = new TestQueryDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        // Create test data
        _missionId = new MethodParamMissionId(Guid.NewGuid().ToString());
        _resourceId = new MethodParamResourceId(42);
        var phase = new MethodParamResponsePhase
        {
            Id = _missionId,
            ResourceId = _resourceId,
            Name = "Test Phase"
        };
        await _context.Phases.AddAsync(phase);
        await _context.SaveChangesAsync();
    }

    void Because() => _result = QueryByMethodParameters(_context, _resourceId, _missionId);

    static MethodParamResponsePhase? QueryByMethodParameters(
        TestQueryDbContext dbContext,
        MethodParamResourceId resourceId,
        MethodParamMissionId missionId) => dbContext.Phases.SingleOrDefault(rp => rp.Id == missionId && rp.ResourceId == resourceId);

    async Task Destroy()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact] void should_find_the_response_phase() => _result.ShouldNotBeNull();
    [Fact] void should_have_correct_mission_id() => _result.Id.Value.ShouldEqual(_missionId.Value);
    [Fact] void should_have_correct_resource_id() => _result.ResourceId.Value.ShouldEqual(_resourceId.Value);
    [Fact] void should_have_correct_name() => _result.Name.ShouldEqual("Test Phase");
}

public record MethodParamEventSourceId(string Value) : ConceptAs<string>(Value);

public record MethodParamMissionId(string Value) : MethodParamEventSourceId(Value)
{
    public static readonly MethodParamMissionId NotSet = new("[not-set]");
    public static implicit operator MethodParamMissionId(string value) => new(value);
}

public record MethodParamResourceId(int Value) : ConceptAs<int>(Value)
{
    public static readonly MethodParamResourceId NotSet = new(0);
    public static implicit operator MethodParamResourceId(int value) => new(value);
}

public class MethodParamResponsePhase
{
    public MethodParamMissionId Id { get; set; } = MethodParamMissionId.NotSet;
    public MethodParamResourceId ResourceId { get; set; } = MethodParamResourceId.NotSet;
    public string? Name { get; set; }
}

public class TestQueryDbContext(DbContextOptions<TestQueryDbContext> options) : DbContext(options)
{
    public DbSet<MethodParamResponsePhase> Phases => Set<MethodParamResponsePhase>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<MethodParamMissionId>()
            .Ignore<MethodParamResourceId>()
            .Ignore<MethodParamEventSourceId>()
            .Entity<MethodParamResponsePhase>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id);
                entity.Property(e => e.ResourceId);
            });
    }
}
