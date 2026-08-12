// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Layered.Ordering.Placing;

namespace Layered.Ordering.Reporting;

/// <summary>
/// Builds the same read model the placing slice builds, from the same event.
/// </summary>
/// <remarks>
/// An application really does grow a second builder for one read model - a reporting slice written beside the slice
/// that owns the model, or a reducer folding what a projection already projects. Screenplay holds one builder for
/// each read model whichever slices declare them, and a document naming two does not compile at all, so the second
/// one has to be turned away and reported rather than emitted.
/// <para>
/// A single project could never show this while a slice kept only its first projection, because the two builders are
/// in different slices and each slice kept its own. That is why it took an application written as several projects
/// to find it.
/// </para>
/// </remarks>
public class OrderReports : IProjectionFor<Order>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<Order> builder) => builder
        .From<OrderPlaced>(_ => _.Set(m => m.Sku).To(e => e.Sku));
}
