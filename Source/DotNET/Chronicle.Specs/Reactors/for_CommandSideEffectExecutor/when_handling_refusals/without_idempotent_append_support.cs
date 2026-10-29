// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_handling_refusals;

public class without_idempotent_append_support : given.a_refusal_with_delivery
{
    Result<ReactorSideEffectFailure> _unkeyedResult;
    Exception? _error;

    void Establish() => _trackedLog.SupportsIdempotentAppends.Returns(false);
    async Task Because()
    {
        _unkeyedResult = await _executor.Execute([_invocation], _context, _store);
        _branch = new EventForEventSourceId("explicit", new RefusalRecorded("keyed")) { IdempotencyKey = new("explicit-key") };
        _error = await Cratis.Specifications.Catch.Exception(async () => await _executor.Execute([_invocation], _context, _store));
    }

    [Fact] void should_allow_unkeyed_events() => _unkeyedResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_assign_keys() => _appended[0].IdempotencyKey.ShouldBeNull();
    [Fact] void should_reject_explicit_keys() => _error.ShouldBeOfExactType<IdempotentAppendsNotSupported>();
    [Fact] void should_not_append_the_keyed_event() => _trackedLog.Attempts.Count.ShouldEqual(1);
}
