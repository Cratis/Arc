// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Gets the events a projection refers to, wherever in its scopes they are named.
/// </summary>
public static class ProjectionEvents
{
    /// <summary>
    /// Gets the names of every event a projection refers to.
    /// </summary>
    /// <param name="projection">The projection to read.</param>
    /// <returns>The names, distinct.</returns>
    public static IEnumerable<string> In(ProjectionModel? projection) =>
        projection is null ? [] : In(projection.Scope).Distinct(StringComparer.Ordinal);

    /// <summary>
    /// Gets the names of every event a scope refers to, including its child and nested scopes.
    /// </summary>
    /// <param name="scope">The scope to read.</param>
    /// <returns>The names.</returns>
    static IEnumerable<string> In(ProjectionScopeModel scope) =>
        scope.From.SelectMany(_ => _.EventTypes)
            .Concat(scope.Joins.Select(_ => _.EventType))
            .Concat(scope.RemovedWith.Select(_ => _.EventType))
            .Concat(scope.RemovedWithJoin.Select(_ => _.EventType))
            .Concat(scope.Children.SelectMany(_ => In(_.Scope)))
            .Concat(scope.Nested.SelectMany(_ => In(_.Scope)));
}
