// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Library;

/// <summary>
/// The slices of the lending feature.
/// </summary>
public static class LibraryLending
{
    /// <summary>
    /// Declares the slice reserving a copy of a book for a member.
    /// </summary>
    /// <returns>The slice.</returns>
    public static SliceModel Reserving() =>
        new(
            "Library.Lending.Reserving",
            "Reserving",
            SliceKind.StateChange,
            null,
            [ReserveBook()],
            [
                new EventModel(
                    "BookReserved",
                    [
                        Declare.Property("Isbn", "ISBN"),
                        Declare.Property("MemberId", "MemberId"),
                        Declare.Property("Tier", "MembershipTier")
                    ],
                    []),
                new EventModel("PremiumReservationGranted", [Declare.Property("MemberId", "MemberId")], [])
            ],
            [],
            [Availability()],
            [],
            []);

    /// <summary>
    /// Declares the slice notifying a member that their reservation is ready.
    /// </summary>
    /// <returns>The slice.</returns>
    public static SliceModel Notifications() =>
        new(
            "Library.Lending.Notifications",
            "Notifications",
            SliceKind.Automation,
            null,
            [],
            [],
            [],
            [],
            [
                new ReactorModel(
                    "ReservationNotifier",
                    ["BookReserved"],
                    false,
                    "Lending/Notifications/Notifications.cs")
            ],
            []);

    /// <summary>
    /// Declares the slice turning a reservation into a restock request.
    /// </summary>
    /// <returns>The slice.</returns>
    /// <remarks>
    /// The reactor carries no source file path, which is what an artifact living in a referenced package looks like.
    /// Emission has to fall back to the vertical slice convention rather than emit a trigger with no body.
    /// </remarks>
    public static SliceModel Restocking() =>
        new(
            "Library.Lending.Restocking",
            "Restocking",
            SliceKind.Translate,
            null,
            [],
            [new EventModel("RestockRequested", [Declare.Property("Isbn", "ISBN")], [])],
            [],
            [],
            [new ReactorModel("RestockRequester", ["BookReserved"], true, null)],
            []);

    /// <summary>
    /// Declares the command reserving a book, which produces one event always and another only for a premium member.
    /// </summary>
    /// <returns>The command.</returns>
    static CommandModel ReserveBook() =>
        new(
            "ReserveBook",
            "Reserves a copy of a book for a member.",
            [
                Declare.Property("Isbn", "ISBN"),
                Declare.Property("MemberId", "MemberId"),
                Declare.Property("Tier", "MembershipTier")
            ],
            new AuthorizationModel(true, []),
            [],
            [
                new ProducesModel(
                    "BookReserved",
                    null,
                    [
                        Declare.From("Isbn", "Isbn"),
                        Declare.From("MemberId", "MemberId"),
                        Declare.From("Tier", "Tier")
                    ]),
                new ProducesModel(
                    "PremiumReservationGranted",
                    new ComparisonCondition("Tier", ComparisonKind.Equal, new LiteralSource("premium")),
                    [Declare.From("MemberId", "MemberId")])
            ],
            null,
            "Lending/Reserving/Reserving.cs");

    /// <summary>
    /// Declares the projection counting how many copies of a title are available.
    /// </summary>
    /// <returns>The projection.</returns>
    static ProjectionModel Availability() =>
        new(
            "Library.Lending.Reserving.AvailabilityProjection",
            "Availability",
            "event-log",
            ProjectionAutoMapMode.Disabled,
            false,
            ProjectionScopeModel.Empty with
            {
                From =
                [
                    new(["BookAddedToInventory"], "$eventSourceId", null, Declare.Map(("available", "$increment"))),
                    new(["BookReserved"], "$eventSourceId", null, Declare.Map(("available", "$decrement")))
                ]
            });
}
