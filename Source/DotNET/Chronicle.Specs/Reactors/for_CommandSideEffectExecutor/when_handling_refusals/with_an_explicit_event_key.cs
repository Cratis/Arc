// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_handling_refusals;

public class with_an_explicit_event_key : given.a_refusal_with_delivery
{
    EventForEventSourceId _explicit;
    EventForEventSourceId _unkeyedRouted;

    void Establish()
    {
        _explicit = new("explicit", new RefusalRecorded("routed"))
        {
            IdempotencyKey = new("explicit-key"), EventStreamType = "type", EventStreamId = "stream",
            EventSourceType = "source", Subject = "subject", Tags = ["tag"], Occurred = DateTimeOffset.UnixEpoch
        };
        _unkeyedRouted = _explicit with { IdempotencyKey = null };
        _branch = new object[] { _explicit, new RefusalRecorded("bare"), _unkeyedRouted };
    }
    async Task Because() => _result = await _executor.Execute([_invocation], _context, _store);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_explicit_key_and_metadata() => ReferenceEquals(_appended[0], _explicit).ShouldBeTrue();
    [Fact] void should_preserve_metadata_when_assigning_a_key() => (_appended[2] with { IdempotencyKey = null }).ShouldEqual(_unkeyedRouted);
    [Fact] void should_count_explicit_events_in_branch_positions() => _appended[1].IdempotencyKey.ShouldEqual(_context.Delivery!.IdempotencyKeyFor("arc:refusal:0:1"));
}
