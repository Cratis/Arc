// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// Defines the registry for compile-time generated query metadata.
/// </summary>
public interface IQueryMetadataRegistry
{
    /// <summary>
    /// Gets all registered query mappings from fully qualified query name to read model type.
    /// </summary>
    IDictionary<string, Type> All { get; }
}
