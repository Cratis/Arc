// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Defines a system that invokes the optional <c>Provide</c> method on a command and returns the values it produces.
/// </summary>
public interface ICommandProvideInvoker
{
    /// <summary>
    /// Invokes the <c>Provide</c> method on the command in the given <see cref="CommandContext"/>, if it has one.
    /// </summary>
    /// <param name="context">The <see cref="CommandContext"/> for the command being handled.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to resolve the <c>Provide</c> method's parameters.</param>
    /// <returns>
    /// The non-null values produced by the <c>Provide</c> method (a single value, or each element of a returned
    /// tuple), or an empty collection when the command has no <c>Provide</c> method.
    /// </returns>
    ValueTask<IReadOnlyList<object>> Invoke(CommandContext context, IServiceProvider serviceProvider);
}
