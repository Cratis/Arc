// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs;

namespace Cratis.Arc.Neo4j.Chronicle;

/// <summary>
/// Translates a backend-neutral <see cref="GraphQuery"/> into one read-only Cypher query.
/// </summary>
/// <remarks>
/// <para>
/// The query matches the roots by the label of the view and returns one map per root, in a column named <c>row</c>,
/// ordered by the key of the root; the first-shape query adds <c>LIMIT 1</c>. A property clause becomes an equality on
/// the root, or <c>IS NULL</c> for a null value, and a related clause an <c>EXISTS</c> subquery along the relation. Clauses
/// only select roots: every relation is read by its own <c>COLLECT</c> subquery that holds every related item, ordered by
/// the key of the far node and then by the identifying property of the edge, one item per edge. A relation of cardinality
/// one takes the first of them with <c>head</c>, which gives null when there is none.
/// </para>
/// <para>
/// Values are never written into the text: each is a parameter, <c>$p0</c>, <c>$p1</c> and so on, converted with
/// <see cref="GraphParameterConverter"/>. Labels, relationship types and property and member names come from the view and
/// are quoted with <see cref="CypherIdentifiers"/>. In <see cref="GraphDatabaseMode.Single"/> mode every node and every
/// relationship of every pattern is constrained to <c>_namespace = $namespace</c>; <see cref="IGraphSession"/> gives the
/// <c>$namespace</c> parameter.
/// </para>
/// <para>
/// The text uses <c>COLLECT</c> subqueries, so the server must be Neo4j 5.6 or later.
/// </para>
/// </remarks>
public static class GraphQueryTranslator
{
    /// <summary>
    /// The name of the column every row of the translated query is returned in.
    /// </summary>
    public const string RowColumn = "row";

    /// <summary>
    /// Translate a query.
    /// </summary>
    /// <param name="query">The <see cref="GraphQuery"/> to translate.</param>
    /// <param name="databaseMode">The <see cref="GraphDatabaseMode"/> of the graph being read.</param>
    /// <returns>The <see cref="GraphCypherQuery"/>.</returns>
    /// <exception cref="InvalidCypherIdentifier">Thrown when a label, type, property or member name cannot be quoted.</exception>
    /// <exception cref="UnknownRelationMember">Thrown when a related clause names a member that is not a relation.</exception>
    public static GraphCypherQuery Translate(GraphQuery query, GraphDatabaseMode databaseMode)
    {
        var translation = new Translation(databaseMode == GraphDatabaseMode.Single);
        var root = translation.NextAlias();
        var rootAlias = $"n{root}";

        var conditions = new List<string>();
        conditions.AddRange(translation.NamespaceConditions(rootAlias));
        conditions.AddRange(query.Clauses.Select(clause => translation.Condition(query.Root, rootAlias, clause)));

        var lines = new List<string> { $"MATCH ({rootAlias}:{CypherIdentifiers.Quote(query.Root.Label)})" };
        if (conditions.Count > 0)
        {
            lines.Add($"WHERE {string.Join(" AND ", conditions)}");
        }

        lines.Add($"RETURN {translation.Projection(query.Root, rootAlias, null)} AS {RowColumn}");
        lines.Add($"ORDER BY {rootAlias}.{CypherIdentifiers.Quote(query.Root.KeyProperty)}");
        if (query.Shape == GraphQueryShape.First)
        {
            lines.Add("LIMIT 1");
        }

        return new(string.Join('\n', lines), translation.Parameters);
    }

    sealed class Translation(bool singleDatabase)
    {
        readonly Dictionary<string, object?> _parameters = [];
        int _aliases;

        public IReadOnlyDictionary<string, object?> Parameters => _parameters;

        public int NextAlias() => _aliases++;

        public IEnumerable<string> NamespaceConditions(params string[] aliases) =>
            singleDatabase
                ? aliases.Select(alias => $"{alias}.{CypherIdentifiers.Quote(GraphParameters.NamespaceProperty)} = ${GraphParameters.Namespace}")
                : [];

        public string Condition(GraphQueryNode node, string alias, GraphQueryClause clause) => clause switch
        {
            GraphPropertyClause property => PropertyCondition(alias, property),
            GraphRelatedClause related => RelatedCondition(node, alias, related),
            _ => throw new UnsupportedGraphQueryClause(clause)
        };

        public string Projection(GraphQueryNode node, string alias, string? edgeAlias)
        {
            var entries = node.Members.Select(member => $"{CypherIdentifiers.Quote(member.Name)}: {Value(node, member, alias, edgeAlias)}");
            return $"{{{string.Join(", ", entries)}}}";
        }

        static GraphQueryRelation RelationOf(GraphQueryNode node, string member) =>
            node.Members.FirstOrDefault(_ => _.Name == member)?.Relation ?? throw new UnknownRelationMember(node.View, member);

        static string Pattern(string near, GraphQueryRelation relation, string edge, string far)
        {
            var type = $"[{edge}:{CypherIdentifiers.Quote(relation.Edge)}]";
            var target = $"({far}:{CypherIdentifiers.Quote(relation.Target.Label)})";
            return relation.Direction == GraphViewDirection.Outgoing
                ? $"({near})-{type}->{target}"
                : $"({near})<-{type}-{target}";
        }

        static string Where(List<string> conditions) => conditions.Count == 0 ? string.Empty : $" WHERE {string.Join(" AND ", conditions)}";

        string PropertyCondition(string alias, GraphPropertyClause clause)
        {
            var property = $"{alias}.{CypherIdentifiers.Quote(clause.Property)}";
            if (clause.Value is null)
            {
                return $"{property} IS NULL";
            }

            var name = $"p{_parameters.Count}";
            _parameters[name] = GraphParameterConverter.ConvertValue(clause.Value);
            return $"{property} = ${name}";
        }

        string RelatedCondition(GraphQueryNode node, string alias, GraphRelatedClause clause)
        {
            var relation = RelationOf(node, clause.Member);
            var index = NextAlias();
            var (edge, far) = ($"e{index}", $"n{index}");
            var conditions = NamespaceConditions(edge, far)
                .Concat(clause.Clauses.Select(nested => PropertyCondition(far, nested)))
                .ToList();
            return $"EXISTS {{ MATCH {Pattern(alias, relation, edge, far)}{Where(conditions)} }}";
        }

        string Value(GraphQueryNode node, GraphQueryMember member, string alias, string? edgeAlias) => member.Kind switch
        {
            GraphViewMemberKind.Property => $"{alias}.{CypherIdentifiers.Quote(member.Source)}",
            GraphViewMemberKind.EdgeProperty => edgeAlias is null ? "null" : $"{edgeAlias}.{CypherIdentifiers.Quote(member.Source)}",
            _ => Relation(alias, member.Relation ?? throw new UnknownRelationMember(node.View, member.Name))
        };

        string Relation(string alias, GraphQueryRelation relation)
        {
            var index = NextAlias();
            var (edge, far) = ($"e{index}", $"n{index}");
            var order = $"{far}.{CypherIdentifiers.Quote(relation.Target.KeyProperty)}";
            if (relation.IdentifyingProperty is not null)
            {
                order = $"{order}, {edge}.{CypherIdentifiers.Quote(relation.IdentifyingProperty)}";
            }

            var items = $"COLLECT {{ MATCH {Pattern(alias, relation, edge, far)}{Where([.. NamespaceConditions(edge, far)])} " +
                $"RETURN {Projection(relation.Target, far, edge)} AS item ORDER BY {order} }}";
            return relation.Cardinality == GraphViewCardinality.One ? $"head({items})" : items;
        }
    }
}
