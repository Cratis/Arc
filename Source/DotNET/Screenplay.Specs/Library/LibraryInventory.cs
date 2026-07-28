// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Library;

/// <summary>
/// The slices of the inventory feature.
/// </summary>
public static class LibraryInventory
{
    /// <summary>
    /// Declares the slice adding copies of a book title to the inventory.
    /// </summary>
    /// <returns>The slice.</returns>
    public static SliceModel Adding() =>
        new(
            "Library.Inventory.Adding",
            "Adding",
            SliceKind.StateChange,
            null,
            [
                new CommandModel(
                    "AddBookTitleToInventory",
                    "Adds copies of a book title to the inventory.",
                    [
                        Declare.Property("Title", "BookTitle"),
                        Declare.Property("Isbn", "ISBN"),
                        Declare.Property("AuthorId", "AuthorId"),
                        Declare.Property("Count", "CopyCount")
                    ],
                    new AuthorizationModel(true, ["Librarian"]),
                    [],
                    [
                        new ProducesModel(
                            "BookAddedToInventory",
                            null,
                            [
                                Declare.From("Title", "Title"),
                                Declare.From("AuthorId", "AuthorId"),
                                Declare.From("Count", "Count")
                            ])
                    ],
                    new ConcurrencyModel(false, null, "Inventory", null, []),
                    "Inventory/Adding/Adding.cs")
            ],
            [
                new EventModel(
                    "BookAddedToInventory",
                    [
                        Declare.Property("Title", "BookTitle"),
                        Declare.Property("AuthorId", "AuthorId"),
                        Declare.Property("Count", "CopyCount")
                    ],
                    [])
            ],
            [],
            null,
            [],
            []);

    /// <summary>
    /// Declares the slice listing the book titles held in the inventory.
    /// </summary>
    /// <returns>The slice.</returns>
    public static SliceModel Listing() =>
        new(
            "Library.Inventory.Listing",
            "Listing",
            SliceKind.StateView,
            null,
            [],
            [],
            [new QueryModel("AllBooks", Declare.Many("Book"), null, [], null)],
            new ProjectionModel(
                "Library.Inventory.Listing.BookProjection",
                "Book",
                "event-log",
                ProjectionAutoMapMode.Enabled,
                false,
                ProjectionScopeModel.Empty with
                {
                    From =
                    [
                        new(
                            ["BookAddedToInventory"],
                            "$eventSourceId",
                            null,
                            Declare.Map(("title", "title"), ("available", "count")))
                    ]
                }),
            [],
            []);
}
