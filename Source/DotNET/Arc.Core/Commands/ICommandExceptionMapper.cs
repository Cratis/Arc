// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Types;

namespace Cratis.Arc.Commands;

/// <summary>
/// Defines convention-discovered mappings of exceptions escaping command execution or response processing to command failures.
/// </summary>
/// <remarks>
/// Implementations are discovered through <see cref="IInstancesOf{T}"/> and resolved from the command's service scope.
/// The pipeline uses the first mapper that accepts an exception, before completing execution scopes.
/// If none accepts it, the existing <see cref="CommandResult.FromException"/> behavior applies.
/// </remarks>
public interface ICommandExceptionMapper
{
    /// <summary>
    /// Tries to map an exception to a command failure without parsing exception messages.
    /// </summary>
    /// <param name="context">The invocation whose execution or response processing failed.</param>
    /// <param name="exception">The exception escaping that invocation.</param>
    /// <param name="result">The mapped failure, preserving the invocation's correlation identifier, or null when declined.</param>
    /// <returns>True when the exception was mapped; false to leave it for another mapper or the default conversion.</returns>
    bool TryMap(CommandContext context, Exception exception, [NotNullWhen(true)] out CommandResult? result);
}
