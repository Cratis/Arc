// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Defines the decisions of the parts of the application for the current context, for clients.
/// </summary>
public interface IUnavailableParts
{
    /// <summary>
    /// Gets the parts that are not available to the current context. An unknown decision is unavailable.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The unavailable parts, ordered by name.</returns>
    /// <exception cref="AvailabilityNotDecidedException">The decision could not be made.</exception>
    Task<IReadOnlyList<UnavailablePart>> Get(CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to changes in what the decisions are made from.
    /// </summary>
    /// <param name="handler">Called when any decision may have changed.</param>
    /// <returns>A disposable that ends the subscription.</returns>
    IDisposable OnChanged(Func<ValueTask> handler);
}
