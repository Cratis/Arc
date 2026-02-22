// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

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
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> scoped to the current request, used to resolve handler dependencies.</param>
    /// <param name="allowedSeverity">Optional maximum validation result severity level to allow.</param>
    /// <returns>A <see cref="CommandResult"/> representing the result of executing the command.</returns>
    Task<CommandResult> Execute(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity = default);

    /// <summary>
    /// Validates the given command without executing it.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> scoped to the current request, used to resolve handler dependencies.</param>
    /// <param name="allowedSeverity">Optional maximum validation result severity level to allow.</param>
    /// <returns>A <see cref="CommandResult"/> representing the validation result.</returns>
    /// <remarks>
    /// This method runs authorization and validation filters but does not invoke the command handler.
    /// Use this for pre-flight validation before executing a command.
    /// </remarks>
    Task<CommandResult> Validate(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity = default);
}
