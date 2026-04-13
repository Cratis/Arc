// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// Defines compile-time generated query metadata for a single assembly.
/// </summary>
/// <remarks>
/// Implementations of this interface are generated at compile time by the Arc source generator.
/// They map fully qualified query names to their read model types, enabling AOT-safe query lookup.
/// </remarks>
public interface IQueryMetadata
{
    /// <summary>
    /// Gets the mapping from fully qualified query names to their read model types.
    /// </summary>
    IReadOnlyDictionary<string, Type> Queries { get; }
}
