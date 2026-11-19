// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.Graphs;
using Cratis.Chronicle.Projections.ModelBound;

namespace Samples.Neo4j;

/// <summary>
/// A person node in the Organization graph Chronicle's Neo4j sink writes.
/// </summary>
/// <param name="PersonId">The key of the node.</param>
/// <param name="Name">The name of the person.</param>
[Graph("Organization")]
[Node]
[FromEvent<PersonRegistered>]
public record Person([Key] Guid PersonId, string Name);
