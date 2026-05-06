// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection;

/// <summary>
/// Defines a service responsible for introspecting discovered command and query endpoints.
/// </summary>
public interface IIntrospectionService
{
    /// <summary>
    /// Gets all discovered command endpoint metadata.
    /// </summary>
    IReadOnlyList<CommandIntrospectionMetadata> Commands { get; }

    /// <summary>
    /// Gets all discovered query endpoint metadata.
    /// </summary>
    IReadOnlyList<QueryIntrospectionMetadata> Queries { get; }
}
