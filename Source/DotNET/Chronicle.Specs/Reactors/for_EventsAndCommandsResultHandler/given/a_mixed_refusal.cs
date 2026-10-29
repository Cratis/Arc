// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Reactors.for_EventsAndCommandsResultHandler.given;

public class a_mixed_refusal : for_CommandSideEffectExecutor.given.a_refusal_with_delivery
{
    protected EventsAndCommandsResultHandler _handler;
    protected Result<ReactorSideEffectFailure> _retry;
    protected object[] _events = [new TestEvent("direct")];
    protected IdempotencyKey?[] _directKeys;

    async Task Establish()
    {
        _handler = new(_executor);
        _store.EventTypes.HasFor(typeof(TestEvent)).Returns(true);
        await new MixedSideEffectsResultHandler().Handle(_context, _store, _events);
        _directKeys = _trackedLog.Attempts[0].Select(envelope => envelope.IdempotencyKey).ToArray();
        _trackedLog.Attempts.Clear();
        _trackedLog.Appended.Clear();
    }

    protected async Task DeliverTwice()
    {
        var effects = _events.Concat([_invocation]).ToArray();
        _result = await _handler.Handle(_context, _store, effects);
        _retry = await _handler.Handle(_context, _store, effects);
    }

    public record TestEvent(string Name);
}
