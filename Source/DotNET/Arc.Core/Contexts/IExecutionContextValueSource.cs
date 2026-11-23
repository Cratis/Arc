// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;

namespace Cratis.Arc.Contexts;

/// <summary>
/// An optional context value source capability that shares values with the gate of a specific bound invocation.
/// </summary>
public interface IExecutionContextValueSource : IContextValueSource
{
    /// <summary>
    /// Reads a value in the invocation's isolated cache. Other executions and metadata reads must not share that cache.
    /// </summary>
    /// <param name="invocation">The same invocation identity used by its availability gate.</param>
    /// <param name="name">The name of the value.</param>
    /// <param name="cancellationToken">The invocation's cancellation token.</param>
    /// <returns>The resolved value or absence.</returns>
    ValueTask<ContextValueRead> ReadForExecution(AvailabilityInvocation invocation, string name, CancellationToken cancellationToken);
}
