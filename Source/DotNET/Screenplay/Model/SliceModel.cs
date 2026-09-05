// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a single vertical slice of the application.
/// </summary>
/// <param name="Namespace">The full namespace the slice lives in, for example <c>Library.Authors.Registration</c>.</param>
/// <param name="Name">The name of the slice.</param>
/// <param name="Kind">The Event Modeling kind of the slice.</param>
/// <param name="Description">The description of the slice, if it has one.</param>
/// <param name="Commands">The commands the slice declares.</param>
/// <param name="Events">The events the slice declares.</param>
/// <param name="Queries">The queries the slice declares.</param>
/// <param name="Projections">The projections the slice declares.</param>
/// <param name="Reactors">The reactors the slice declares.</param>
/// <param name="Constraints">The constraints the slice declares.</param>
/// <remarks>
/// A slice declares as many projections as its behavior needs. The read model a screen binds to and the one a
/// command reads to decide belong to the same behavior, so they belong to the same slice - and an application
/// routinely writes both, alongside whatever a fold over events builds.
/// </remarks>
public record SliceModel(
    string Namespace,
    string Name,
    SliceKind Kind,
    string? Description,
    IEnumerable<CommandModel> Commands,
    IEnumerable<EventModel> Events,
    IEnumerable<QueryModel> Queries,
    IEnumerable<ProjectionModel> Projections,
    IEnumerable<ReactorModel> Reactors,
    IEnumerable<ConstraintModel> Constraints)
{
    /// <summary>
    /// Gets the screens the slice ends in.
    /// </summary>
    /// <remarks>
    /// Screens are the only part of a slice that is not recovered from the compilation, so they are carried
    /// alongside what is rather than within it - a slice that nothing knows the screens of is still a slice.
    /// </remarks>
    public IEnumerable<ScreenModel> Screens { get; init; } = [];

    /// <summary>
    /// Gets the scenarios the slice is specified by.
    /// </summary>
    /// <remarks>
    /// A scenario is recovered from a namespace beneath the slice's own rather than from the slice's, so it arrives
    /// once every namespace has been read and which of them declare a slice is known - which is after the slice
    /// itself exists.
    /// </remarks>
    public IEnumerable<SpecificationModel> Specifications { get; init; } = [];

    /// <summary>
    /// Creates a slice that declares nothing, for use as a starting point.
    /// </summary>
    /// <param name="namespace">The full namespace the slice lives in.</param>
    /// <param name="name">The name of the slice.</param>
    /// <param name="kind">The Event Modeling kind of the slice.</param>
    /// <returns>The empty <see cref="SliceModel"/>.</returns>
    public static SliceModel Empty(string @namespace, string name, SliceKind kind) =>
        new(@namespace, name, kind, null, [], [], [], [], [], []);
}
