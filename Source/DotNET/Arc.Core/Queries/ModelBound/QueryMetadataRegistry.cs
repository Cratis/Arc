// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// Represents the registry for compile-time generated query metadata.
/// </summary>
/// <remarks>
/// Source-generated module initializers call <see cref="Register"/> at assembly load time.
/// The static collection is shared across all instances, so any <see cref="QueryMetadataRegistry"/>
/// instance surfaced through <see cref="IQueryMetadataRegistry"/> sees all registered metadata.
/// </remarks>
public class QueryMetadataRegistry : IQueryMetadataRegistry
{
    static readonly ConcurrentDictionary<string, Type> _queries = [];

    /// <inheritdoc/>
    public IDictionary<string, Type> All => _queries;

    /// <summary>
    /// Registers a query mapping if it has not already been registered.
    /// </summary>
    /// <param name="queryName">The fully qualified query name.</param>
    /// <param name="readModelType">The read model type for the query.</param>
    public static void Register(string queryName, Type readModelType) => _queries.TryAdd(queryName, readModelType);
}

