// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines a renderer that can render a query.
/// </summary>
public interface IQueryPerformer
{
    /// <summary>
    /// Gets the name of the query the performer can perform.
    /// </summary>
    QueryName Name { get; }

    /// <summary>
    /// Gets the fully qualified name of the query the performer can perform.
    /// </summary>
    FullyQualifiedQueryName FullyQualifiedName { get; }

    /// <summary>
    /// Gets the type of the query performer.
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Gets the type of read model the query performer is for.
    /// </summary>
    Type ReadModelType { get; }

    /// <summary>
    /// Gets the location the query is at.
    /// </summary>
    /// <remarks>
    /// This is used to determine which renderer should render a given query based on its location.
    /// </remarks>
    IEnumerable<string> Location { get; }

    /// <summary>
    /// Gets the dependencies required by the renderer.
    /// </summary>
    IEnumerable<Type> Dependencies { get; }

    /// <summary>
    /// Gets the query parameters for the performer.
    /// </summary>
    /// <remarks>
    /// This includes parameters that are not dependencies, typically those that come from query string or request parameters.
    /// </remarks>
    QueryParameters Parameters { get; }

    /// <summary>
    /// Gets a value indicating whether anonymous access is allowed for this query.
    /// </summary>
    bool AllowsAnonymousAccess { get; }

    /// <summary>
    /// Checks if the current user is authorized to perform this query.
    /// </summary>
    /// <param name="context">The context for the query.</param>
    /// <returns>True if authorized, false if unauthorized.</returns>
    bool IsAuthorized(QueryContext context);

    /// <summary>
    /// Renders the given query.
    /// </summary>
    /// <param name="context">The context for the query to render.</param>
    /// <returns>The result of rendering the query.</returns>
    ValueTask<object?> Perform(QueryContext context);
}
