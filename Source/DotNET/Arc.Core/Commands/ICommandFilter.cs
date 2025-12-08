// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Defines a filter that can be applied to commands.
/// </summary>
public interface ICommandFilter
{
    /// <summary>
    /// Called when a command is executed.
    /// </summary>
    /// <param name="context">The <see cref="CommandContext"/> for the command being executed.</param>
    /// <returns>The <see cref="CommandResult"/>.</returns>
    Task<CommandResult> OnExecution(CommandContext context);
}
