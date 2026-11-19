// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs;

namespace Cratis.Arc.Neo4j.Chronicle.given;

/// <summary>
/// Gives the views of the Organization graph as the Chronicle client discovers them, read once and shared read-only.
/// </summary>
public static class OrganizationViews
{
    /// <summary>
    /// The name of the graph.
    /// </summary>
    public const string Graph = "Organization";

    static readonly Lazy<IGraphViews> _views = new(Discover);

    /// <summary>
    /// Gets the views.
    /// </summary>
    public static IGraphViews Views => _views.Value;

    /// <summary>
    /// Get the resolved query node of a view by its name.
    /// </summary>
    /// <param name="view">The name of the view.</param>
    /// <returns>The <see cref="GraphQueryNode"/>.</returns>
    public static GraphQueryNode Node(string view) => Views.NodeOf(Graph, view);

    /// <summary>
    /// Build a query over a view.
    /// </summary>
    /// <param name="view">The name of the view.</param>
    /// <param name="shape">The <see cref="GraphQueryShape"/>.</param>
    /// <param name="clauses">The clauses.</param>
    /// <returns>The <see cref="GraphQuery"/>.</returns>
    public static GraphQuery Query(string view, GraphQueryShape shape = GraphQueryShape.List, params GraphQueryClause[] clauses) =>
        new(Graph, shape, Node(view), clauses);

    static IGraphViews Discover()
    {
        var artifacts = new GraphArtifacts(
            [typeof(Person), typeof(Group), typeof(MemberOf), typeof(PartOf), typeof(HasRole)],
            [
                typeof(PersonWithGroups), typeof(GroupMembership), typeof(GroupSummary), typeof(GroupWithMembers),
                typeof(GroupWithParent), typeof(MemberSummary), typeof(PersonRoles), typeof(RoleAssignment)
            ]);
        return GraphViews.Discover(artifacts);
    }
}
