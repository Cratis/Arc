---
title: Read a Neo4j graph through typed views in Cratis Arc
description: Take IGraph in a model-bound query and read Chronicle graph views from Neo4j as typed, nested records, with tenancy, single-database mode, testing and limits.
---

A graph view reads a graph back as a typed read model: a record anchored at one node, with the node's properties, relations along edges to other views, and properties of the edge that was followed. Chronicle declares and checks views; `Cratis.Arc.Neo4j.Chronicle` runs them against Neo4j. A model-bound query takes an `IGraph`, calls `Nodes<TView>()` and gets nested records back, without writing Cypher.

Views fit a read that has a fixed shape: a group with its members, a person with the groups they belong to. When you need a traversal a view cannot express, such as a path of any length, write the Cypher yourself with `IGraphSession`; see [querying](./querying.md).

## Install

Graph views need Neo4j set up first; follow [getting started](./getting-started.md). Then add the package:

```bash
dotnet add package Cratis.Arc.Neo4j.Chronicle
```

It depends on `Cratis.Arc.Neo4j` and on the Chronicle client, `Cratis.Chronicle`, where views are declared. It does not need a running Chronicle server or Arc's Chronicle integration. `Cratis.Arc.Neo4j` itself does not depend on Chronicle, and a host that does not add this package behaves exactly as before.

Add graph views after Neo4j:

```csharp
builder.AddCratisArc(configureBuilder: arc => arc.WithNeo4j().WithNeo4jGraphViews());
```

On a service collection, call `AddCratisNeo4jGraphViews()` after `AddCratisNeo4j()`. Use one style or the other, not both.

The server must be Neo4j 5.6 or later, because the translated query uses `COLLECT` subqueries.

## Declare the views

Views are declared in your application with the Chronicle client, over the nodes and edges of the graph that a graph projection writes. The [Chronicle graph projection documentation](/chronicle/projections/graph-projections/) describes nodes, edges and views in full. This group view lists the members of a group and when each joined:

```csharp
[ViewOf<Person>]
public record MemberSummary(PersonId Id, PersonName? Name, [FromEdge] DateTimeOffset Since);
```

`ViewOf<TNode>` anchors a view at a node. `Follows<TEdge>` makes a member a relation along an edge to another view, and `FromEdge` reads a property of the edge that was followed. A view that breaks a declaration rule is logged as a warning when the views are discovered and left out; querying it throws `NotAGraphView`.

## Query a view

A model-bound query takes `IGraph` like any other dependency. The view can carry `[ReadModel]` itself, so the query returns it directly:

```csharp
[ReadModel]
[ViewOf<Group>]
public record GroupWithMembers(GroupId Id, GroupName? Name, [Follows<MemberOf>] IReadOnlyList<MemberSummary> Members)
{
    public static Task<GroupWithMembers?> WithMembers(GroupId groupId, IGraph graph) =>
        graph.Nodes<GroupWithMembers>().Where(_ => _.Id, groupId).FirstOrDefault();

    public static Task<IReadOnlyList<GroupWithMembers>> HavingMember(PersonId personId, IGraph graph) =>
        graph.Nodes<GroupWithMembers>()
            .WhereRelated<MemberSummary>(_ => _.Members, members => members.Where(_ => _.Id, personId))
            .ToList();
}
```

- `Where` keeps the roots whose property equals a value.
- `WhereRelated` keeps the roots that have at least one related item, optionally one whose properties equal given values.
- `ToList` returns every matching root, and `FirstOrDefault` the first one in key order, or `null`.

Clauses only select roots. A relation always holds every related item: in `HavingMember`, each group lists all of its members, not only the person asked for. A relation that is a collection holds an empty list when there is nothing related, and a single relation holds `null`. A property the graph does not have, such as the name of a node that only an edge has created, reads as `null`.

`IGraph` is scoped, like `IGraphSession`, so take it in a query, a command's `Provide()` method or a scoped service, never in a singleton.

## What runs on the server

Each query becomes one read-only Cypher query on the database of the current tenant. For `GroupWithMembers` without clauses it is:

```cypher
MATCH (n0:`Group`)
RETURN {`id`: n0.`groupId`, `name`: n0.`name`, `members`: COLLECT { MATCH (n0)<-[e1:`MEMBER_OF`]-(n1:`Person`) RETURN {`id`: n1.`personId`, `name`: n1.`name`, `since`: e1.`since`} AS item ORDER BY n1.`personId` }} AS row
ORDER BY n0.`groupId`
```

Values never become part of the text. Every clause value is a parameter, converted the way the graph stores it: a concept as the value it wraps, a `Guid` as its string, an enum by name and a `decimal` as a `double`. Labels, relationship types and property names come from the declared graph and are quoted; one that is empty or holds a backtick is refused with `InvalidCypherIdentifier`. Temporal values come back as ISO 8601 text and are built into `DateTimeOffset`, `DateOnly` and the other types your view declares.

## Tenancy

Views read through `IGraphSession`, so they follow its [tenancy](./tenancy.md): the database is the one selected for the current tenant. In `Single` database mode every node and every relationship of the query is constrained to `_namespace = $namespace`, the namespace of the current tenant, so a view never reads another tenant's graph.

## Test with a substitute

`Cratis.Arc.Neo4j.Chronicle.Testing` replaces `IGraph` without a Neo4j server:

```bash
dotnet add package Cratis.Arc.Neo4j.Chronicle.Testing
```

```csharp
var services = new ServiceCollection();
var graph = services.UseGraphSubstitute();
graph.Returns(new GroupWithMembers(groupId, new GroupName("Engineering"), []));

var group = await GroupWithMembers.WithMembers(groupId, graph);
```

- `Returns<TView>(views)` sets up the views a query for that view answers with. A view nothing was set up for answers with none.
- The substitute builds clauses as production does, so a query on a type that is not a valid view throws `NotAGraphView`, but it does not evaluate them: `ToList` answers with every view set up and `FirstOrDefault` with the first.
- `Executions` lists every query that ran as the backend-neutral `GraphQuery`, with its shape and clauses. Assert on it to check what a query selects.
- `UseGraphSubstitute(views)` takes the `IGraphViews` to use; without it, views are discovered from the loaded assemblies.

To evaluate the clauses against a graph built from events, use Chronicle's `GraphScenario` and hand its graph to the service collection with `UseGraph(scenario.Graph)`.

## Limits

- Nesting is finite. A view cannot contain itself, and there are no variable-length paths.
- Roots and related items are ordered by node key, then by the identity of an identified edge. There is no other ordering, no aggregation, no `or` and no ranges.
- Reads are eventually consistent with the event log and can see stale elements while a projection replays. Do not decide a command's invariant from a view.
- Views only read. A graph changes when events are appended and a graph projection writes them.
