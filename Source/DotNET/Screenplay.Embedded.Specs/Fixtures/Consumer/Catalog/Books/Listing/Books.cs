// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;

namespace Company.Library.Catalog.Books.Listing;

/// <summary>
/// Represents a fixture book.
/// </summary>
/// <param name="Title">The title.</param>
[ReadModel]
public record Books(string Title)
{
    /// <summary>
    /// Gets the fixture books.
    /// </summary>
    /// <returns>The books.</returns>
    public static IEnumerable<Books> All() => [new("A Library")];
}
