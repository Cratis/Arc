// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    }

    /// <summary>
    /// Filter with an explicit cast: (MissionId)rp.Id == missionId.
    /// This creates an expression with op_Equality that references MissionId types.
    /// </summary>
    void Because() => _result = _context.ResponsePhases.SingleOrDefault(rp => (MissionId)rp.Id == _missionId);

    [Fact] void should_find_the_response_phase() => _result.ShouldNotBeNull();
    [Fact] void should_have_correct_mission_id() => _result!.Id.Value.ShouldEqual(_missionId.Value);
}
