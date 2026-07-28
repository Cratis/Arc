// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Library;

/// <summary>
/// A slice whose projection exercises every block the projection definition language has.
/// </summary>
/// <remarks>
/// The library application is what a real application looks like; this is what the hardest corner of the language
/// looks like. Every event the projection names is declared, so that a round trip has nothing to warn about.
/// </remarks>
public static class OrderingSlice
{
    /// <summary>
    /// Declares the slice.
    /// </summary>
    /// <returns>The slice.</returns>
    public static SliceModel Build() =>
        new(
            "Library.Ordering.Tracking",
            "Tracking",
            SliceKind.StateView,
            null,
            [],
            [.. Events()],
            [new QueryModel("AllOrders", Declare.Many("Order"), null, [], null)],
            Projection(),
            [],
            []);

    /// <summary>
    /// Declares every event the projection observes.
    /// </summary>
    /// <returns>The events.</returns>
    static IEnumerable<EventModel> Events() =>
    [
        new("OrderPlaced", [Declare.Property("CustomerId", "Uuid")], []),
        new("OrderShipped", [Declare.Property("Carrier", "String")], []),
        new("OrderCancelled", [], []),
        new("CustomerRegistered", [Declare.Property("Name", "String")], []),
        new("CustomerAccountClosed", [], []),
        new("LineItemAdded", [Declare.Property("Amount", "Decimal")], []),
        new("LineItemRemoved", [], [])
    ];

    /// <summary>
    /// Declares the projection.
    /// </summary>
    /// <returns>The projection.</returns>
    static ProjectionModel Projection() =>
        new(
            "Library.Ordering.Tracking.OrderProjection",
            "Order",
            "orders",
            ProjectionAutoMapMode.Disabled,
            false,
            new ProjectionScopeModel(
                [
                    new(
                        ["OrderPlaced", "OrderShipped"],
                        "$composite(OrderKey, CustomerId=customerId, Number=orderNumber)",
                        "customerId",
                        Declare.Map(
                            ("placedBy", "$causedBy.name"),
                            ("anonymous", "$causedBy"),
                            ("id", "$eventSourceId"),
                            ("status", "$value(placed)"),
                            ("versions", "$increment"),
                            ("pending", "$decrement")))
                ],
                new ProjectionEveryModel(
                    Declare.Map(("lastUpdated", "$eventContext(occurred)")),
                    false,
                    ProjectionAutoMapMode.Enabled),
                [new("Customer", "CustomerRegistered", "customerId", Declare.Map(("customerName", "name")))],
                [Items()],
                [Shipping()],
                [new("OrderCancelled", null, null)],
                [new("CustomerAccountClosed", null, null)]));

    /// <summary>
    /// Declares the child collection of line items.
    /// </summary>
    /// <returns>The child scope.</returns>
    static ProjectionChildScopeModel Items() =>
        new(
            "Items",
            "lineNumber",
            ProjectionAutoMapMode.Enabled,
            ProjectionScopeModel.Empty with
            {
                From =
                [
                    new(
                        ["LineItemAdded"],
                        "lineNumber",
                        "orderId",
                        Declare.Map(("total", "$add(amount)"), ("occurrences", "$count"), ("refunded", "$subtract(amount)")))
                ],
                RemovedWith = [new("LineItemRemoved", "lineNumber", "orderId")]
            });

    /// <summary>
    /// Declares the nested shipping details.
    /// </summary>
    /// <returns>The nested scope.</returns>
    static ProjectionChildScopeModel Shipping() =>
        new(
            "Shipping",
            string.Empty,
            ProjectionAutoMapMode.Inherit,
            ProjectionScopeModel.Empty with
            {
                From = [new(["OrderShipped"], null, null, Declare.Map(("carrier", "carrier")))]
            });
}
