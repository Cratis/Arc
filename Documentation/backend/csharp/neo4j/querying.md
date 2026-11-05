---
title: Query a Neo4j graph from Cratis Arc
description: Run Cypher through the tenant-aware IGraphSession, map rows to your own types, and open a write session in the rare case you need one.
---

`IGraphSession` runs Cypher on the database of the current tenant. Inject it into a model-bound query, a command's `Provide()` method or any scoped service.

## Map rows to types

`Query<T>` runs a query and maps every row to `T`, and `QuerySingle<T>` maps the first row, or gives `null` when there is none:

```csharp
var members = await graph.Query<GroupMember>(
    "MATCH (p:Person)-[:MEMBER_OF]->(:Group { groupId: $groupId }) RETURN p.personId AS personId, p.name AS name",
    new { groupId });
```

Parameters are an object whose public properties are the parameters, or a dictionary. A query that returns one column maps that column when `T` is a primitive, a concept or a list of those; otherwise the columns are matched to the constructor parameters or settable properties of `T` by name, ignoring case. A column holding a node or a relationship contributes its properties, so `RETURN p` maps a `Person` node to a record. See [concepts](./concepts.md) for how values convert.

## Use the driver directly

`Run` returns the driver's `IResultCursor`, and `Session` exposes the underlying `IAsyncSession` for transaction functions. Use `Parameters` to get the converted parameters, including `$namespace`, when you run a query through the driver yourself:

```csharp
var result = await graph.Session.ExecuteReadAsync(async runner =>
{
    var cursor = await runner.RunAsync(cypher, graph.Parameters(new { groupId }));
    return await cursor.ToListAsync();
});
```

## Sessions are read sessions

The scoped `IGraphSession` opens with `AccessMode.Read`, so the server refuses a query that writes. A graph is what events leave behind: change it by appending events that a graph projection turns into nodes and relationships, never by writing from a query. When you really need a write session, for instance to maintain data no projection owns, open one:

```csharp
public class Maintenance(IGraphSessionFactory sessions)
{
    public async Task Rename(PersonId personId, PersonName name)
    {
        await using var graph = sessions.Open(AccessMode.Write);
        await graph.Run("MATCH (p:Person { personId: $personId }) SET p.name = $name", new { personId, name });
    }
}
```

Dispose a session you open. The scoped session is disposed with the scope.

## Lifetime

The session and the connection behind it are resolved once per scope, from the tenant of that scope. The driver and its connection pool are shared for the life of the application, so opening a session is cheap.
