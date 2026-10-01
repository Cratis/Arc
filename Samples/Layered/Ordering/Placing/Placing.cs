// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Projections.ModelBound;

namespace Layered.Ordering.Placing;

/// <summary>
/// Represents a command placing an order.
/// </summary>
/// <param name="CustomerId">Who is ordering.</param>
/// <param name="Sku">What is being ordered.</param>
[Command]
public record PlaceOrder(CustomerId CustomerId, string Sku)
{
    /// <summary>
    /// Handles the command by stating that the order was placed.
    /// </summary>
    /// <returns>The <see cref="OrderPlaced"/> event.</returns>
    public OrderPlaced Handle() => new(Sku);
}

/// <summary>
/// Represents what was ordered.
/// </summary>
/// <remarks>
/// The slice below this one builds the same read model a second way, which is the other shape this fixture exists
/// for. Screenplay holds one builder for each read model however many slices declare one, so a document naming two
/// does not compile at all - and only an application really written that way reaches the rule.
/// </remarks>
[ReadModel]
[FromEvent<OrderPlaced>]
public record Order
{
    /// <summary>
    /// Gets what was ordered.
    /// </summary>
    [SetFrom<OrderPlaced>(nameof(OrderPlaced.Sku))]
    public string Sku { get; init; } = string.Empty;
}
