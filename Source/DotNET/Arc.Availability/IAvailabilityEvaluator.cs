// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;

namespace Cratis.Arc.Availability;

/// <summary>
/// Defines the engine that decides whether a part of the model is available for a request.
/// </summary>
/// <remarks>
/// Unknown denies, absence is unknown, and a failure is an exception, never a denial.
/// </remarks>
public interface IAvailabilityEvaluator
{
    /// <summary>
    /// Evaluates one gate.
    /// </summary>
    /// <param name="gate">The gate expression.</param>
    /// <param name="context">The context of the request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The result.</returns>
    /// <exception cref="UndefinedAvailabilityException">The gate uses a block that is not defined.</exception>
    /// <exception cref="CircularAvailabilityException">Blocks build on each other in a cycle.</exception>
    /// <exception cref="RequiredContextValueAbsentException">A required context value is absent.</exception>
    /// <exception cref="ContextValueProviderFailedException">A context value provider failed.</exception>
    ValueTask<AvailabilityResult> Evaluate(AvailabilityExpression gate, ContextValueResolver context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates the gates down a path (module, feature, slice, then command, query or screen). Every gate must hold.
    /// </summary>
    /// <param name="gates">The gates, outermost first. None means available.</param>
    /// <param name="context">The context of the request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The result.</returns>
    ValueTask<AvailabilityResult> EvaluatePath(IEnumerable<AvailabilityExpression> gates, ContextValueResolver context, CancellationToken cancellationToken = default);
}
