// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    /// <param name="reactorType">The type of the reactor that returned the commands, used to resolve system execution roles.</param>
    /// <returns>
    /// A <see cref="Result{TError}"/> holding a <see cref="ReactorSideEffectFailure"/> describing the first
    /// command that failed, or a success result when every command executed successfully.
    /// </returns>
    /// <remarks>
    /// Commands execute sequentially in order within a single service scope. Execution stops at the first command
    /// that fails and that failure is returned. There is no transaction spanning the commands — any events appended
    /// by earlier, successful commands remain committed. When the reactor is marked with
    /// <see cref="ExecuteCommandsAsSystemAttribute"/>, the commands execute as a trusted system actor holding the declared roles.
    /// </remarks>
    Task<Result<ReactorSideEffectFailure>> Execute(IEnumerable<object> commands, Type reactorType);
}
