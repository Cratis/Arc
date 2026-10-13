// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Reactors;

/// <summary>
/// Defines a system that executes commands returned from a reactor as side effects.
/// </summary>
public interface ICommandSideEffectExecutor
{
    /// <summary>
    /// Executes the given commands as side effects within a dedicated service scope.
    /// </summary>
    /// <param name="commands">The commands to execute.</param>
    /// <returns>
    /// A <see cref="Result{TError}"/> holding a <see cref="ReactorSideEffectFailure"/> describing the first
    /// command that failed, or a success result when every command executed successfully.
    /// </returns>
    /// <remarks>
    /// Commands execute sequentially in order within a single service scope. Execution stops at the first command
    /// that fails and that failure is returned. There is no transaction spanning the commands — any events appended
    /// by earlier, successful commands remain committed.
    /// </remarks>
    Task<Result<ReactorSideEffectFailure>> Execute(IEnumerable<object> commands);

    /// <summary>
    /// Executes the given commands as side effects within a dedicated service scope, resolving system execution roles
    /// from the reactor that returned them.
    /// </summary>
    /// <param name="commands">The commands to execute.</param>
    /// <param name="reactorType">The type of the reactor that returned the commands, used to resolve system execution roles.</param>
    /// <returns>
    /// A <see cref="Result{TError}"/> holding a <see cref="ReactorSideEffectFailure"/> describing the first
    /// command that failed, or a success result when every command executed successfully.
    /// </returns>
    /// <remarks>
    /// Behaves like <see cref="Execute(IEnumerable{object})"/>, except that when the reactor is marked with
    /// <see cref="ExecuteCommandsAsSystemAttribute"/>, the commands execute as a trusted system actor holding the
    /// declared roles. When the reactor is not marked, this behaves identically to <see cref="Execute(IEnumerable{object})"/>.
    /// </remarks>
    Task<Result<ReactorSideEffectFailure>> Execute(IEnumerable<object> commands, Type reactorType);

    /// <summary>
    /// Executes commands and invocation branches using the reactor's event routing for branch productions.
    /// </summary>
    /// <param name="commands">The ordered commands or invocations.</param>
    /// <param name="reactorContext">The reactor context supplying identity and event routing.</param>
    /// <param name="eventStore">The event store to which branch events are appended.</param>
    /// <returns>The first failure, or success after all commands or a handled refusal.</returns>
    /// <remarks>
    /// A handled refusal stops the remaining invocations. Custom executors retain their existing execution path
    /// unless they implement this overload to support refusal branches.
    /// </remarks>
    Task<Result<ReactorSideEffectFailure>> Execute(IEnumerable<object> commands, ReactorContext reactorContext, IEventStore eventStore) =>
        Execute(commands, reactorContext.Reactor.GetType());
}
