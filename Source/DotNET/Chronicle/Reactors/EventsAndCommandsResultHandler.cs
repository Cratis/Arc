// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.DependencyInjection;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Reactors;

/// <summary>
/// Appends the events in a mixed reactor result before executing its commands in order.
/// </summary>
/// <param name="executor">The executor for returned command side effects.</param>
[Singleton]
public class EventsAndCommandsResultHandler(ICommandSideEffectExecutor executor) : IReactorSideEffectHandler
{
    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, object value) =>
        reactorContext.EventStore is not null && CanHandle(reactorContext, reactorContext.EventStore, value);

    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, IEventStore eventStore, object value) =>
        EventsAndCommands.TryCreate(eventStore.EventTypes, value, out _);

    /// <inheritdoc/>
    public async Task<Result<ReactorSideEffectFailure>> Handle(ReactorContext reactorContext, IEventStore eventStore, object value)
    {
        if (!EventsAndCommands.TryCreate(eventStore.EventTypes, value, out var effects))
        {
            return Result.Failed(new ReactorSideEffectFailure([new AppendFailure([], false, ["A mixed reactor result must contain only registered events and commands, with at least one of each."], [])]));
        }

        var result = await new MixedSideEffectsResultHandler().Handle(reactorContext, eventStore, effects.Events);
        if (!result.IsSuccess)
        {
            return result;
        }

        return await executor.Execute(effects.Commands, reactorContext.Reactor.GetType());
    }
}
