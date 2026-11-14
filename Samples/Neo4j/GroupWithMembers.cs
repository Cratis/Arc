// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Graphs;

namespace Samples.Neo4j;

/// <summary>
/// A group with every member, read from the graph through a typed view instead of Cypher.
/// </summary>
/// <param name="Id">The identity of the group; the key of the node.</param>
/// <param name="Name">The name of the group, or null for a group only an edge has created.</param>
/// <param name="Members">The members, ordered by the key of the person.</param>
[ReadModel]
[ViewOf<Group>]
public record GroupWithMembers(GroupId Id, GroupName? Name, [Follows<MemberOf>] IReadOnlyList<MemberSummary> Members)
{
    /// <summary>
    /// Get a group with its members.
    /// </summary>
    /// <param name="groupId">The group to get.</param>
    /// <param name="graph">The <see cref="IGraph"/> over the graph database of the current tenant.</param>
    /// <returns>The group, or null when there is none.</returns>
    /// <remarks>
    /// The query becomes parameterized Cypher on the database of the current tenant, constrained to the namespace in
    /// <c>Single</c> database mode, and the rows are built into the records with their concepts.
    /// </remarks>
    public static Task<GroupWithMembers?> WithMembers(GroupId groupId, IGraph graph) =>
        graph.Nodes<GroupWithMembers>().Where(_ => _.Id, groupId).FirstOrDefault();

    /// <summary>
    /// Get the groups a person is a member of, each with all of its members.
    /// </summary>
    /// <param name="personId">The person.</param>
    /// <param name="graph">The <see cref="IGraph"/> over the graph database of the current tenant.</param>
    /// <returns>The groups, ordered by key.</returns>
    public static Task<IReadOnlyList<GroupWithMembers>> HavingMember(PersonId personId, IGraph graph) =>
        graph.Nodes<GroupWithMembers>()
            .WhereRelated<MemberSummary>(_ => _.Members, members => members.Where(_ => _.Id, personId))
            .ToList();
}
