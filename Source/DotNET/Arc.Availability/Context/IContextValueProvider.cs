// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.Context;

/// <summary>
/// Defines where a provided context value comes from.
/// </summary>
public interface IContextValueProvider
{
    /// <summary>
    /// Gets the names of the other context values this provider reads, used to reject cycles before a request runs.
    /// </summary>
    IReadOnlyCollection<string> Dependencies { get; }

    /// <summary>
    /// Provides the value.
    /// </summary>
    /// <param name="context">The context of the request, for reading values this one is built from.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The outcome. Return absent when the value does not exist; throw when the source failed.</returns>
    ValueTask<ContextValueOutcome> Provide(IContextValueResolver context, CancellationToken cancellationToken);
}
