// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_querying_with_concepts;

[Collection(nameof(ConceptAsQueryingCollection))]
public class when_querying_with_auto_conversion_and_multiple_concept_conditions : given.a_response_phase_database
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

    Task Because()
    {
        _result = _context.ResponsePhases
            .SingleOrDefault(rp => rp.Id == _missionId && rp.ResourceId == _resourceId);
        return Task.CompletedTask;
    }

    [Fact] void should_find_the_response_phase() => _result.ShouldNotBeNull();
    [Fact] void should_have_correct_mission_id() => _result!.Id.Value.ShouldEqual(_missionId.Value);
    [Fact] void should_have_correct_resource_id() => _result!.ResourceId.Value.ShouldEqual(_resourceId.Value);
    [Fact] void should_have_correct_name() => _result!.Name.ShouldEqual("Test Phase");
}
