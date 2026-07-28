// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents everything declared within one projection scope - the projection itself, or a child or nested object.
/// </summary>
/// <param name="From">The blocks observing specific event types.</param>
/// <param name="Every">The block applying to every observed event, if the scope declares one.</param>
/// <param name="Joins">The blocks joining data from other events.</param>
/// <param name="Children">The child collections of the scope.</param>
/// <param name="Nested">The nested objects of the scope.</param>
/// <param name="RemovedWith">The blocks removing instances when an event occurs.</param>
/// <param name="RemovedWithJoin">The blocks removing joined instances when an event occurs.</param>
public record ProjectionScopeModel(
    IEnumerable<ProjectionFromModel> From,
    ProjectionEveryModel? Every,
    IEnumerable<ProjectionJoinModel> Joins,
    IEnumerable<ProjectionChildScopeModel> Children,
    IEnumerable<ProjectionChildScopeModel> Nested,
    IEnumerable<ProjectionRemoveModel> RemovedWith,
    IEnumerable<ProjectionRemoveModel> RemovedWithJoin)
{
    /// <summary>
    /// Represents a scope declaring nothing at all.
    /// </summary>
    public static readonly ProjectionScopeModel Empty = new([], null, [], [], [], [], []);
}
