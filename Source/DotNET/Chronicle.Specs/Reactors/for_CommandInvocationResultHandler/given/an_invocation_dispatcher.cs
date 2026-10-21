// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors.SideEffects;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandInvocationResultHandler.given;

public class an_invocation_dispatcher : for_CommandSideEffectExecutor.given.a_refused_invocation
{
    protected ReactorSideEffectHandlers _dispatcher;
    protected CommandInvocationResultHandler _handler;
    protected List<object[]> _batches = [];

    void Establish()
    {
        _handler = new(_executor);
        var instances = Substitute.For<IInstancesOf<IReactorSideEffectHandler>>();
        IReactorSideEffectHandler[] handlers =
        [
            new EventResultHandler(), new EventsResultHandler(), new EventForEventSourceIdResultHandler(),
            new EventsForEventSourceIdResultHandler(), new MixedSideEffectsResultHandler(),
            new CommandResultHandler(_executor), new CommandsResultHandler(_executor),
            new EventsAndCommandsResultHandler(_executor), _handler
        ];
        instances.GetEnumerator().Returns(_ => handlers.AsEnumerable().GetEnumerator());
        _dispatcher = new(instances);
    }
}
