// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Represents a query exposed by the read model type that declares it and the method name, so no query name has to be
/// spelled out and nested read models need no special naming.
/// </summary>
/// <param name="ReadModelType">The read model type that declares the query method.</param>
/// <param name="Method">The query method name.</param>
sealed record McpQueryReference(Type ReadModelType, string Method)
{
    /// <summary>
    /// Determines whether a query performer performs the referenced query.
    /// </summary>
    /// <param name="performer">The query performer.</param>
    /// <returns>True if the performer is for the referenced read model and method; otherwise false.</returns>
    public bool Matches(IQueryPerformer performer) =>
        performer.ReadModelType == ReadModelType && string.Equals(performer.Name.Value, Method, StringComparison.Ordinal);

    /// <inheritdoc/>
    public override string ToString() => $"{ReadModelType.FullName}.{Method}";
}
