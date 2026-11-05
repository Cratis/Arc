---
title: Test Neo4j queries in Cratis Arc
description: Substitute the tenant-aware graph session in specifications and command scenarios with Cratis.Arc.Neo4j.Testing, without a Neo4j server.
---

Install `Cratis.Arc.Neo4j.Testing` in your specification project to answer graph queries with rows you set up:

```bash
dotnet add package Cratis.Arc.Neo4j.Testing
```

`UseGraphSessionSubstitute()` replaces the session in a service collection, for instance the `Services` of a command scenario, and returns a `GraphSessionSubstitute`:

```csharp
var services = new ServiceCollection();
var graph = services.UseGraphSessionSubstitute();
graph.Returns("MATCH (p:Person)", new GroupMember(personId, new PersonName("Ada")));

var members = await GroupMember.MembersOf(groupId, graph);
```

- `Returns<T>(fragment, rows)` sets up the rows a query answers with when its Cypher contains the fragment, ignoring case. A query nothing was set up for returns no rows.
- `Executions` lists the queries that were run, with their parameters as the driver would get them: concepts unwrapped and `$namespace` included. Assert on it to check a query was filtered or bound correctly.
- `Namespace`, `DatabaseName`, `DatabaseMode` and `AccessMode` can be set to exercise code that depends on them.

`UseGraphSession(session)` replaces the session with one you built, for instance an `NSubstitute` substitute for `IGraphSession`. Either replacement also answers `IGraphSessionFactory.Open`.

`Run` and `Session` need a real driver and throw on the substitute, so keep application code that maps rows on `Query` and `QuerySingle`, or cover the driver calls with an integration test against Neo4j.
