// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_querying_with_concepts;

/// <summary>
/// Tests the scenario where an expression filter with explicit ConceptAs casts is passed.
/// This is the pattern that was causing "The operands for operator 'Equal' do not match
/// the parameters of method 'op_Equality'" errors.
/// </summary>
[Collection(nameof(ConceptAsQueryingCollection))]
public class when_querying_with_explicit_concept_cast_in_filter : given.a_response_phase_database
{
    MissionId _missionId;
    ResourceId _resourceId;
    ResponsePhase? _result;
    Expression<Func<ResponsePhase, bool>> _filter;

    async Task Establish()
    {
        _missionId = MissionId.New();
        _resourceId = ResourceId.New();
        var phase = new ResponsePhase
        {
            Id = _missionId,
            ResourceId = _resourceId,
            Name = "Test Phase"
        };
        await _context.ResponsePhases.AddAsync(phase);
        await _context.SaveChangesAsync();

        // Create a filter with an explicit cast - simulating the pattern:
        // (MissionId)p.MissionId == @p.missionId
        // This creates an expression with op_Equality that references MissionId types
        _filter = CreateFilterWithCast(_missionId);
    }

    Task Because()
    {
        _result = _context.ResponsePhases
            .Where(_filter)
            .SingleOrDefault();
        return Task.CompletedTask;
    }

    [Fact] void should_find_the_response_phase() => _result.ShouldNotBeNull();
    [Fact] void should_have_correct_mission_id() => _result!.Id.Value.ShouldEqual(_missionId.Value);

    static Expression<Func<ResponsePhase, bool>> CreateFilterWithCast(MissionId missionId)
    {
        // Build expression manually to include explicit cast: (MissionId)rp.Id == missionId
        var param = Expression.Parameter(typeof(ResponsePhase), "rp");
        var propertyAccess = Expression.Property(param, nameof(ResponsePhase.Id));

        // Add explicit cast to MissionId (this simulates what some code generators produce)
        var castToMissionId = Expression.Convert(propertyAccess, typeof(MissionId));

        // Create the constant for missionId
        var constant = Expression.Constant(missionId, typeof(MissionId));

        // Create the equality comparison - this will use op_Equality(MissionId, MissionId)
        var equality = Expression.Equal(castToMissionId, constant);

        return Expression.Lambda<Func<ResponsePhase, bool>>(equality, param);
    }
}
