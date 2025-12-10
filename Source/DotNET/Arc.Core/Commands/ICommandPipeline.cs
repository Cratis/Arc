// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Defines a system can execute commands.
/// </summary>
public interface ICommandPipeline
{
    /// <summary>
    /// Executes the given command.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <returns>A <see cref="CommandResult"/> representing the result of executing the command.</returns>
    Task<CommandResult> Execute(object command);

    /// <summary>
    /// Validates the given command without executing it.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <returns>A <see cref="CommandResult"/> representing the validation result.</returns>
    /// <remarks>
    /// This method runs authorization and validation filters but does not invoke the command handler.
    /// Use this for pre-flight validation before executing a command.
    /// </remarks>
    Task<CommandResult> Validate(object command);
}
