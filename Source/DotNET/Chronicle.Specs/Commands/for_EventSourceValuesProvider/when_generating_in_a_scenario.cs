// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_EventSourceValuesProvider;

public class when_generating_in_a_scenario : Specification
{
    readonly CommandScenario<GenerateIdentity> _scenario = new();
    readonly Guid _eventSourceId = Guid.NewGuid();
    readonly Guid _handlerId = Guid.NewGuid();
    CommandResult _result;

    void Establish() => _scenario.Generate(_eventSourceId, _handlerId);

    async Task Because() => _result = await _scenario.Execute(new GenerateIdentity());

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] async Task should_share_the_queue_between_allocation_and_the_handler() =>
        await _scenario.ShouldHaveAppendedEvent<GenerateIdentity, IdentityGenerated>(
            new EventSourceId(_eventSourceId), e => e.GeneratedId == _handlerId);

    void Destroy() => _scenario.Dispose();
}
