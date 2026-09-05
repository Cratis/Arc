// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Library;

/// <summary>
/// The slices of the authors feature.
/// </summary>
public static class LibraryAuthors
{
    /// <summary>
    /// Declares the slice registering an author.
    /// </summary>
    /// <returns>The slice.</returns>
    public static SliceModel Registration() =>
        new(
            "Library.Authors.Registration",
            "Registration",
            SliceKind.StateChange,
            "Registers a new author in the library.",
            [
                new CommandModel(
                    "RegisterAuthor",
                    "Registers a new author.",
                    [Declare.Property("Name", "AuthorName")],
                    null,
                    [
                        new("Name", ValidationRuleKind.NotEmpty, null, "An author must have a name"),
                        new("Name", ValidationRuleKind.Max, 100, null)
                    ],
                    [new ProducesModel("AuthorRegistered", null, [Declare.From("Name", "Name")])],
                    null,
                    "Authors/Registration/Registration.cs")
            ],
            [new EventModel("AuthorRegistered", [Declare.Property("Name", "AuthorName")], ["audit", "authors"])],
            [],
            [],
            [],
            [new UniquePropertyConstraintModel("UniqueAuthorName", "Name", "AuthorRegistered")]);

    /// <summary>
    /// Declares the slice listing the registered authors.
    /// </summary>
    /// <returns>The slice.</returns>
    public static SliceModel Listing() =>
        new(
            "Library.Authors.Listing",
            "Listing",
            SliceKind.StateView,
            null,
            [],
            [],
            [
                new QueryModel("AllAuthors", Declare.Many("Author"), null, [], null),
                new QueryModel(
                    "AuthorById",
                    Declare.Maybe("Author"),
                    Declare.Property("Id", "AuthorId"),
                    [],
                    null)
            ],
            [
                new ProjectionModel(
                    "Library.Authors.Listing.AuthorProjection",
                    "Author",
                    "event-log",
                    ProjectionAutoMapMode.Enabled,
                    false,
                    ProjectionScopeModel.Empty with
                    {
                        From = [new(["AuthorRegistered"], "$eventSourceId", null, Declare.Map(("name", "name")))]
                    })
            ],
            [],
            []);
}
