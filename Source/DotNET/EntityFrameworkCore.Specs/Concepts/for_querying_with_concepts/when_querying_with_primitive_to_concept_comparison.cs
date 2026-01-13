// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_querying_with_concepts;

/// <summary>
/// Tests the scenario where a primitive Guid is compared to a ConceptAs entity property.
/// This was causing "The binary operator Equal is not defined for the types 'System.Guid'
/// and 'MissionId'" errors when the expression visitor transformed one side but not the other.
/// </summary>
[Collection(nameof(ConceptAsQueryingCollection))]
public class when_querying_with_primitive_to_concept_comparison : given.a_response_phase_database
{
    MissionId _missionId;
    ResourceId _resourceId;
    Guid _guidValue;
    ResponsePhase? _result;

    async Task Establish()
    {
        _missionId = MissionId.New();
        _resourceId = ResourceId.New();
        _guidValue = _missionId.Value;

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
    /// Compare a primitive Guid to the entity's MissionId property
    /// The rewriter must handle the type mismatch: Guid vs MissionId.
    /// </summary>
    void Because() => _result = _context.ResponsePhases.SingleOrDefault(rp => (MissionId)_guidValue == rp.Id);

    [Fact] void should_find_the_response_phase() => _result.ShouldNotBeNull();
    [Fact] void should_have_correct_mission_id() => _result.Id.Value.ShouldEqual(_missionId.Value);
}
