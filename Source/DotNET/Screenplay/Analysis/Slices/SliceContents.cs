// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Slices;

/// <summary>
/// Collects everything a slice declares, while its namespace is being read.
/// </summary>
public class SliceContents
{
    /// <summary>
    /// Gets the commands the slice declares.
    /// </summary>
    public IList<CommandModel> Commands { get; } = [];

    /// <summary>
    /// Gets the events the slice declares.
    /// </summary>
    public IList<EventModel> Events { get; } = [];

    /// <summary>
    /// Gets the queries the slice declares, each with the type that declared it.
    /// </summary>
    public IList<DeclaredQuery> Queries { get; } = [];

    /// <summary>
    /// Gets the reactors the slice declares.
    /// </summary>
    public IList<ReactorModel> Reactors { get; } = [];

    /// <summary>
    /// Gets the constraints the slice declares.
    /// </summary>
    public IList<ConstraintModel> Constraints { get; } = [];

    /// <summary>
    /// Gets or sets the single projection the slice declares.
    /// </summary>
    public ProjectionModel? Projection { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the slice declares an aggregate root.
    /// </summary>
    /// <remarks>
    /// An aggregate root governs a change to the system, which is what a state change slice is, so a slice holding
    /// one is a state change even when no command sits beside it.
    /// </remarks>
    public bool HasAggregateRoot { get; set; }

    /// <summary>
    /// Gets the number of artifacts collected so far.
    /// </summary>
    /// <remarks>
    /// What one type contributed is the difference between this before it was read and after, which is how a
    /// declaration is tied to what came out of it without every recognizer having to say so itself.
    /// </remarks>
    public int Count =>
        Commands.Count +
        Events.Count +
        Queries.Count +
        Reactors.Count +
        Constraints.Count +
        (Projection is null ? 0 : 1);

    /// <summary>
    /// Gets a value indicating whether the slice declares nothing at all.
    /// </summary>
    public bool IsEmpty => Count == 0;
}
