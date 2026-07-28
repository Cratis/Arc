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
    /// Gets a value indicating whether the slice declares nothing at all.
    /// </summary>
    public bool IsEmpty =>
        Commands.Count == 0 &&
        Events.Count == 0 &&
        Queries.Count == 0 &&
        Reactors.Count == 0 &&
        Constraints.Count == 0 &&
        Projection is null;
}
