// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Defines a handler for processing command response values.
/// </summary>
public interface ICommandResponseValueHandler
{
    /// <summary>
    /// Determines whether this handler can handle the given value.
    /// </summary>
    /// <param name="commandContext">Context for the command resulting in the value.</param>
    /// <param name="value">Value to evaluate.</param>
    /// <returns>True if the handler can handle the value, false otherwise.</returns>
    bool CanHandle(CommandContext commandContext, object value);

    /// <summary>
    /// Handles the given value.
    /// </summary>
    /// <param name="commandContext">Context for the command resulting in the value.</param>
    /// <param name="value">Value to handle.</param>
    /// <returns><see cref="CommandResult"/> representing the result of handling the value.</returns>
    Task<CommandResult> Handle(CommandContext commandContext, object value);
}
