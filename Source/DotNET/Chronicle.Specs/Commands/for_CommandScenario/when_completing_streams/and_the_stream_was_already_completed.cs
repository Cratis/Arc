// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_completing_streams;

public class and_the_stream_was_already_completed : Specification
{
    CommandScenario<CompletePeriodAgain> _scenario;
    CommandResult _result;
    EventSourceId _id;

    async Task Establish()
    {
        _id = EventSourceId.New();
        _scenario = new();
        await _scenario.Execute(new(_id));
    }

    async Task Because() => _result = await _scenario.Execute(new(_id));

    [Fact] void should_succeed_on_redelivery() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_record_both_successful_completions() => _scenario.CompletedStreams.Count.ShouldEqual(2);
    void Destroy() => _scenario.Dispose();
}
