// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// Test double for <see cref="IQueryMetadata"/> that wraps a dictionary.
/// </summary>
/// <param name="queries">The query dictionary to expose.</param>
public class StubQueryMetadata(IReadOnlyDictionary<string, Type> queries) : IQueryMetadata
{
    /// <inheritdoc/>
    public IReadOnlyDictionary<string, Type> Queries { get; } = queries;
}
