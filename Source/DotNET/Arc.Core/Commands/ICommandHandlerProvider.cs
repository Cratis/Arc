// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Arc.Commands;

/// <summary>
/// Defines a provider for command handlers.
/// </summary>
public interface ICommandHandlerProvider
{
    /// <summary>
    /// Gets the collection of command handlers.
    /// </summary>
    IEnumerable<ICommandHandler> Handlers { get; }

    /// <summary>
    /// Tries to get a handler for the given command.
    /// </summary>
    /// <param name="command">Command to handle.</param>
    /// <param name="handler">Handler for the command, if found.</param>
    /// <returns>True if a handler was found; otherwise, false.</returns>
    bool TryGetHandlerFor(object command, [NotNullWhen(true)] out ICommandHandler? handler);
}
