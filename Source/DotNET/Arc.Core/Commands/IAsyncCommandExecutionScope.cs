// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Defines an execution scope with asynchronous initialization before command filters run.
/// </summary>
/// <remarks>
/// Implementations are discovered as <see cref="ICommandExecutionScope"/>. All synchronous
/// <see cref="ICommandExecutionScope.Begin"/> calls run first, establishing ambient state. Then asynchronous
/// initialization runs sequentially in the same scope order, after context values are built and before filters.
/// Use synchronous Begin for ambient state: changes to AsyncLocal values inside an asynchronous method do not
/// flow back to its caller. Completion follows the ordinary execution-scope lifecycle, including initialization
/// failure or cancellation. Validation-only calls do not begin or complete execution scopes.
/// </remarks>
public interface IAsyncCommandExecutionScope : ICommandExecutionScope
{
    /// <summary>
    /// Initializes the scope asynchronously, honoring <see cref="CommandContext.CancellationToken"/>.
    /// </summary>
    /// <param name="context">The command context, including its built values and cancellation token.</param>
    /// <returns>The initialization task.</returns>
    Task BeginAsync(CommandContext context);
}
