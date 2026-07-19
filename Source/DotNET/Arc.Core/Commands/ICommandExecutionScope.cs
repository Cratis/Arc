// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Defines a scope that participates in the execution of a command, beginning before the command's filters and
/// handler run and completing with the final <see cref="CommandResult"/> — whether the command succeeded or not.
/// </summary>
/// <remarks>
/// Implementations are discovered automatically and invoked by the command pipeline around every command execution.
/// This is the extension point for bracketing a command with cross-cutting lifetime concerns, such as making the
/// command a transactional scope. <see cref="Begin"/> is deliberately synchronous so ambient state an implementation
/// establishes — for example an <c>AsyncLocal</c>-based unit of work — flows to the command's execution.
/// </remarks>
public interface ICommandExecutionScope
{
    /// <summary>
    /// Begins the scope for the command about to execute.
    /// </summary>
    /// <param name="context">The <see cref="CommandContext"/> for the command.</param>
    void Begin(CommandContext context);

    /// <summary>
    /// Completes the scope with the final result of the command, called for every outcome — success, validation
    /// failure or exception. Implementations can mutate the <see cref="CommandResult"/> to reflect the outcome of
    /// completing the scope.
    /// </summary>
    /// <param name="context">The <see cref="CommandContext"/> for the command.</param>
    /// <param name="result">The final, mutable <see cref="CommandResult"/> for the command.</param>
    /// <returns>Awaitable task.</returns>
    Task Complete(CommandContext context, CommandResult result);
}
