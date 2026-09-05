// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Projections;

/// <summary>
/// Collects everything one scope of a fluent projection declares, while the chain is being read.
/// </summary>
public class FluentScope
{
    /// <summary>
    /// Gets the blocks observing specific event types.
    /// </summary>
    public IList<ProjectionFromModel> From { get; } = [];

    /// <summary>
    /// Gets the blocks joining data from other events.
    /// </summary>
    public IList<ProjectionJoinModel> Joins { get; } = [];

    /// <summary>
    /// Gets the child collections of the scope.
    /// </summary>
    public IList<ProjectionChildScopeModel> Children { get; } = [];

    /// <summary>
    /// Gets the nested objects of the scope.
    /// </summary>
    public IList<ProjectionChildScopeModel> Nested { get; } = [];

    /// <summary>
    /// Gets the blocks removing instances when an event occurs.
    /// </summary>
    public IList<ProjectionRemoveModel> RemovedWith { get; } = [];

    /// <summary>
    /// Gets the blocks removing joined instances when an event occurs.
    /// </summary>
    public IList<ProjectionRemoveModel> RemovedWithJoin { get; } = [];

    /// <summary>
    /// Gets or sets the block applying to every observed event.
    /// </summary>
    public ProjectionEveryModel? Every { get; set; }

    /// <summary>
    /// Gets or sets how automatic property mapping applies.
    /// </summary>
    public ProjectionAutoMapMode AutoMap { get; set; } = ProjectionAutoMapMode.Inherit;

    /// <summary>
    /// Gets or sets the identifier of the sequence the projection observes.
    /// </summary>
    public string? Sequence { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the projection observes every event type in the system.
    /// </summary>
    public bool SubscribesToAllEvents { get; set; }

    /// <summary>
    /// Gets a value indicating whether the scope declares nothing at all.
    /// </summary>
    public bool IsEmpty =>
        From.Count == 0 &&
        Joins.Count == 0 &&
        Children.Count == 0 &&
        Nested.Count == 0 &&
        RemovedWith.Count == 0 &&
        RemovedWithJoin.Count == 0 &&
        Every is null;

    /// <summary>
    /// Turns what was collected into the model of a scope.
    /// </summary>
    /// <returns>The <see cref="ProjectionScopeModel"/>.</returns>
    public ProjectionScopeModel ToModel() => new(From, Every, Joins, Children, Nested, RemovedWith, RemovedWithJoin);
}
