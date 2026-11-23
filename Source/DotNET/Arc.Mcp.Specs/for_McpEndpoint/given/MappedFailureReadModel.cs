// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>A read model whose query fails before returning data.</summary>
[ReadModel]
public record MappedFailureReadModel
{
    /// <summary>Fails the query.</summary>
    /// <returns>No result.</returns>
    /// <exception cref="InvalidOperationException">Always fails for the mapping specification.</exception>
    public static MappedFailureReadModel All() => throw new InvalidOperationException("private failure detail");
}
