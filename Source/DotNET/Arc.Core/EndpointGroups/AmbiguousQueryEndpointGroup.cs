// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.EndpointGroups;

/// <summary>
/// The exception thrown when query overloads belong to different endpoint groups.
/// </summary>
/// <param name="query">The ambiguous query.</param>
public class AmbiguousQueryEndpointGroup(FullyQualifiedQueryName query)
    : Exception($"Query '{query}' has overloads in different endpoint groups. The query performer must identify its exact query method through IAuthorizationQueryTarget.");
