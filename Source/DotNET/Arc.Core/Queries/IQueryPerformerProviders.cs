// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines a provider for query performers.
/// </summary>
public interface IQueryPerformerProviders
{
    /// <summary>
    /// Gets the collection of query performers.
    /// </summary>
    IEnumerable<IQueryPerformer> Performers { get; }

    /// <summary>
    /// Tries to get a performer for the given query.
    /// </summary>
    /// <param name="query">Query to render.</param>
    /// <param name="performer">Performer for the query, if found.</param>
    /// <returns>True if a performer was found; otherwise, false.</returns>
    bool TryGetPerformersFor(FullyQualifiedQueryName query, [NotNullWhen(true)] out IQueryPerformer? performer);
}
