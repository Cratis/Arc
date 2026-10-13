// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.DependencyInjection;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Reactors;

/// <summary>
/// Executes a returned invocation with its refusal branches.
/// </summary>
/// <param name="executor">The executor for the protected command pipeline and branch productions.</param>
[Singleton]
public class CommandInvocationResultHandler(ICommandSideEffectExecutor executor) : IReactorSideEffectHandler
{
    /// <inheritdoc/>
    public bool CanHandleReturnType(Type type) =>
        type == typeof(CommandInvocation) || typeof(IEnumerable<CommandInvocation>).IsAssignableFrom(type);

    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, object value) => value is CommandInvocation;

    /// <inheritdoc/>
    public Task<Result<ReactorSideEffectFailure>> Handle(ReactorContext reactorContext, IEventStore eventStore, object value) =>
        executor.Execute([value], reactorContext, eventStore);
}
