// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// Registry for compile-time generated query metadata.
/// </summary>
/// <remarks>
/// Source-generated module initializers call <see cref="Register"/> at assembly load time
/// so that <see cref="QueryPerformerProvider"/> can find all query types without reflection-based
/// assembly scanning, enabling AOT-safe operation.
/// </remarks>
public static class QueryMetadataRegistry
{
    static readonly List<IQueryMetadata> _metadata = [];

    /// <summary>
    /// Gets all registered <see cref="IQueryMetadata"/> instances.
    /// </summary>
    public static IEnumerable<IQueryMetadata> All => _metadata;

    /// <summary>
    /// Registers a <see cref="IQueryMetadata"/> instance.
    /// </summary>
    /// <param name="metadata">The metadata to register.</param>
    public static void Register(IQueryMetadata metadata) => _metadata.Add(metadata);

    /// <summary>
    /// Clears all registered metadata. Intended for use in tests only.
    /// </summary>
    public static void ClearForTesting() => _metadata.Clear();
}
