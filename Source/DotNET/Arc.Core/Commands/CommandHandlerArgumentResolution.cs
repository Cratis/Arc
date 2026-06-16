// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Represents the outcome of resolving the arguments for a command handler.
/// </summary>
/// <param name="Arguments">The resolved arguments to pass to the handler, in declaration order.</param>
/// <param name="ControlResult">
/// The <see cref="CommandResult"/> produced by any short-circuiting control values returned from the
/// command's <c>Provide</c> method. Successful when nothing short-circuited.
/// </param>
public record CommandHandlerArgumentResolution(IReadOnlyList<object?> Arguments, CommandResult ControlResult)
{
    /// <summary>
    /// Gets a value indicating whether argument resolution short-circuited command execution.
    /// </summary>
    public bool IsShortCircuited => !ControlResult.IsSuccess;
}
