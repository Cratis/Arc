// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.DependencyInjection;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Reactors;

/// <summary>
/// Represents a <see cref="IReactorSideEffectHandler"/> that executes multiple commands returned from a reactor.
/// </summary>
/// <param name="executor">The <see cref="ICommandSideEffectExecutor"/> used to execute the commands.</param>
[Singleton]
public class CommandsResultHandler(ICommandSideEffectExecutor executor) : IReactorSideEffectHandler
{
    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, object value) =>
        value is IEnumerable<object> commands && commands.Any() && commands.All(command => command?.GetType().IsCommand() == true);

    /// <inheritdoc/>
    public Task<Result<ReactorSideEffectFailure>> Handle(ReactorContext reactorContext, IEventStore eventStore, object value) =>
        executor.Execute((IEnumerable<object>)value, reactorContext.Reactor.GetType());
}
