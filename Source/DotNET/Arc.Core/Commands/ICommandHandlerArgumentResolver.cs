// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands;

/// <summary>
/// Defines a system that resolves the arguments for a command handler, including any values produced by the
/// command's <c>Provide</c> method, and surfaces any short-circuiting result it produces.
/// </summary>
public interface ICommandHandlerArgumentResolver
{
    /// <summary>
    /// Resolves the arguments for the given command handler.
    /// </summary>
    /// <param name="handler">The <see cref="ICommandHandler"/> to resolve arguments for.</param>
    /// <param name="context">The <see cref="CommandContext"/> for the command being handled.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to resolve arguments not produced by <c>Provide</c>.</param>
    /// <param name="allowedSeverity">The maximum validation result severity to allow before short-circuiting.</param>
    /// <returns>
    /// A <see cref="CommandHandlerArgumentResolution"/> carrying the resolved arguments, or a short-circuiting
    /// <see cref="CommandResult"/> when the command's <c>Provide</c> method produced a blocking control value.
    /// </returns>
    ValueTask<CommandHandlerArgumentResolution> Resolve(
        ICommandHandler handler,
        CommandContext context,
        IServiceProvider serviceProvider,
        ValidationResultSeverity? allowedSeverity);
}
