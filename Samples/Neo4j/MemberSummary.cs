// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs;

namespace Samples.Neo4j;

/// <summary>
/// A member of a group as a group view reads it: the person and when they joined.
/// </summary>
/// <param name="Id">The identity of the person; the key of the node.</param>
/// <param name="Name">The name of the person, or null for a person only an edge has created.</param>
/// <param name="Since">When the person joined, read from the edge that was followed.</param>
[ViewOf<Person>]
public record MemberSummary(PersonId Id, PersonName? Name, [FromEdge] DateTimeOffset Since);
