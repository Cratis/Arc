// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Defines a system that knows about <see cref="ICommandResponseValueHandler"/> and can use them to handle response values.
/// </summary>
public interface ICommandResponseValueHandlers
{
    /// <summary>
    /// Determines whether any handler can handle the given value.
    /// </summary>
    /// <param name="context">The <see cref="CommandContext"/> for the command that produced the value.</param>
    /// <param name="value">Value to evaluate.</param>
    /// <returns>True if any handler can handle the value, false otherwise.</returns>
    bool CanHandle(CommandContext context, object value);

    /// <summary>
    /// Handles the given value.
    /// </summary>
    /// <param name="context">The <see cref="CommandContext"/> for the command that produced the value.</param>
    /// <param name="value">Value to handle.</param>
    /// <returns><see cref="CommandResult"/> representing the result of handling the value.</returns>
    Task<CommandResult> Handle(CommandContext context, object value);
}
