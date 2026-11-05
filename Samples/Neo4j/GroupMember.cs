// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j;
using Cratis.Arc.Queries.ModelBound;

namespace Samples.Neo4j;

/// <summary>
/// A person who is a member of a group, read from the graph Chronicle's Neo4j sink keeps for the namespace of the current tenant.
/// </summary>
/// <param name="PersonId">The identity of the person.</param>
/// <param name="Name">The name of the person.</param>
[ReadModel]
public record GroupMember(PersonId PersonId, PersonName Name)
{
    /// <summary>
    /// Get the members of a group.
    /// </summary>
    /// <param name="groupId">The group to get the members of.</param>
    /// <param name="graph">The <see cref="IGraphSession"/> on the database of the current tenant.</param>
    /// <returns>The members, by name.</returns>
    /// <remarks>
    /// The group id binds as the primitive stored on the node, and every row becomes a <see cref="GroupMember"/> with
    /// its concepts rebuilt. In a database per namespace nothing else is needed to stay inside the tenant. With
    /// <c>DatabaseMode</c> set to <c>Single</c>, add <c>AND g._namespace = $namespace</c> and <c>p._namespace = $namespace</c>.
    /// </remarks>
    public static Task<IReadOnlyList<GroupMember>> MembersOf(GroupId groupId, IGraphSession graph) =>
        graph.Query<GroupMember>(
            "MATCH (p:Person)-[:MEMBER_OF]->(g:Group { groupId: $groupId }) RETURN p.personId AS personId, p.name AS name ORDER BY p.name",
            new { groupId });
}
