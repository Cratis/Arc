// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing_streams;

public class with_an_unspecified_event_source : given.a_pending_completion
{
    void Establish() => _context.Values[WellKnownCommandContextKeys.EventSourceId] = EventSourceId.Unspecified;

    async Task Because()
    {
        _result.MergeWith(await Enroll());
        await _scope.Complete(_context, _result);
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_all_sources_completion() => _eventLog.Received(1).CompleteStream("completion", "period");
}
