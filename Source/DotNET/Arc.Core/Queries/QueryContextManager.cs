// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an implementation of <see cref="QueryContext"/>.
/// </summary>
[Singleton]
public class QueryContextManager : IQueryContextManager
{
    static readonly AsyncLocal<QueryContext> _context = new();

    /// <inheritdoc/>
    public QueryContext Current => _context.Value ?? QueryContext.NotSet;

    /// <inheritdoc/>
    public void Set(QueryContext context) => _context.Value = context;
}
