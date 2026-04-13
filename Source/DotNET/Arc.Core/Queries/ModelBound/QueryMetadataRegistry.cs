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
    static readonly ConcurrentBag<IQueryMetadata> _metadata = [];

    /// <inheritdoc/>
    public IEnumerable<IQueryMetadata> All => _metadata;

    /// <summary>
    /// Registers a <see cref="IQueryMetadata"/> instance.
    /// </summary>
    /// <param name="metadata">The metadata to register.</param>
    public static void Register(IQueryMetadata metadata) => _metadata.Add(metadata);
}

