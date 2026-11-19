// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.Graphs;
using Cratis.Chronicle.Projections.ModelBound;

namespace Samples.Neo4j;

/// <summary>
/// A group node in the Organization graph Chronicle's Neo4j sink writes.
/// </summary>
/// <param name="GroupId">The key of the node.</param>
/// <param name="Name">The name of the group.</param>
[Graph("Organization")]
[Node]
[FromEvent<GroupCreated>]
public record Group([Key] Guid GroupId, string Name);
