// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Library;

/// <summary>
/// A library shaped application, expressed purely as a model.
/// </summary>
/// <remarks>
/// This is what a source analysis of a real Arc application is expected to recover. Building it by hand keeps every
/// emission specification honest about the one thing it is testing - what the document says - without dragging in a
/// compiler, a container or a mocked runtime.
/// </remarks>
public static class LibraryApplication
{
    /// <summary>
    /// The name of the domain and module the application declares.
    /// </summary>
    public const string Name = "Library";

    /// <summary>
    /// Builds the model of the application.
    /// </summary>
    /// <returns>The <see cref="ApplicationModel"/>.</returns>
    public static ApplicationModel Build() =>
        new(
            Name,
            Name,
            LibraryConcepts.All(),
            LibraryConcepts.Policies(),
            Slices(),
            []);

    /// <summary>
    /// Declares every slice of the application.
    /// </summary>
    /// <returns>The slices.</returns>
    public static IEnumerable<SliceModel> Slices() =>
    [
        LibraryAuthors.Registration(),
        LibraryAuthors.Listing(),
        LibraryInventory.Adding(),
        LibraryInventory.Listing(),
        LibraryLending.Reserving(),
        LibraryLending.Notifications(),
        LibraryLending.Restocking()
    ];
}
