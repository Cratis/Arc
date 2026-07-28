// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Library;

/// <summary>
/// The concepts and policies a library application declares at the document level.
/// </summary>
public static class LibraryConcepts
{
    /// <summary>
    /// Declares every concept of the application.
    /// </summary>
    /// <returns>The concepts.</returns>
    public static IEnumerable<ConceptModel> All() =>
    [
        new("AuthorId", ScreenplayPrimitive.Uuid, false, [], []),
        new(
            "AuthorName",
            ScreenplayPrimitive.String,
            true,
            [],
            [
                new("Value", ValidationRuleKind.NotEmpty, null, "An author name is required"),
                new("Value", ValidationRuleKind.Max, 200, null)
            ]),
        new("BookTitle", ScreenplayPrimitive.String, false, [], []),
        new("CopyCount", ScreenplayPrimitive.Int, false, [], []),
        new("ISBN", ScreenplayPrimitive.String, false, [], []),
        new("MemberId", ScreenplayPrimitive.Uuid, false, [], []),
        new("MembershipTier", ScreenplayPrimitive.Enum, false, ["Standard", "Premium"], [])
    ];

    /// <summary>
    /// Declares every policy of the application.
    /// </summary>
    /// <returns>The policies.</returns>
    /// <remarks>
    /// Only the role a slice actually names is declared here. The policy an authenticated caller alone satisfies is
    /// left out on purpose, so that emission has to keep the document self consistent by declaring it itself.
    /// </remarks>
    public static IEnumerable<PolicyModel> Policies() =>
    [
        new("Librarian", true, "Librarian")
    ];
}
