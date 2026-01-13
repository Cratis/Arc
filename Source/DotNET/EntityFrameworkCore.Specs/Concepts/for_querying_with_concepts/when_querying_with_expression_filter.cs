// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_querying_with_concepts;

[Collection(nameof(ConceptAsQueryingCollection))]
public class when_querying_with_expression_filter : given.a_response_phase_database
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

        // Create a filter expression that may include casts
        _filter = rp => rp.Id == _missionId;
    }

    Task Because()
    {
        // Using Where with an expression filter - this is the pattern that was failing
        _result = _context.ResponsePhases
            .Where(_filter)
            .SingleOrDefault();
        return Task.CompletedTask;
    }

    [Fact] void should_find_the_response_phase() => _result.ShouldNotBeNull();
    [Fact] void should_have_correct_mission_id() => _result!.Id.Value.ShouldEqual(_missionId.Value);
    [Fact] void should_have_correct_resource_id() => _result!.ResourceId.Value.ShouldEqual(_resourceId.Value);
    [Fact] void should_have_correct_name() => _result!.Name.ShouldEqual("Test Phase");
}
