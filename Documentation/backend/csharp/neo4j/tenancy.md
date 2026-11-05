---
title: Select Neo4j databases by tenant
description: Select Neo4j databases in Arc C# applications using tenant namespaces, Chronicle-compatible naming, custom resolvers or single-database mode.
---

Cratis Arc selects a Neo4j database using the configured event store and the namespace of the current tenant. Chronicle's Neo4j graph sink uses the same algorithm to choose the database it writes, so an Arc application reads the graph of its own namespace and sees nothing from the others.

Establish the tenant context before resolving a session. `IGraphSession` is scoped, so a singleton must not capture it; otherwise it can keep using the database selected for an earlier scope. Database naming does not authorize access. Tenant resolution and access checks must prevent unauthorized tenant selection. See the [tenancy overview](../tenancy/index.md) for that setup, or [tenancy end to end](../tenancy/end-to-end.md) to compare Neo4j with MongoDB, EF Core and Chronicle.

## The namespace

`IGraphNamespaceResolver` maps the current tenant to a namespace. The default maps like the Chronicle integration does: a named tenant is the namespace with the same name, and both `TenantId.NotSet` and `TenantId.Default` are Chronicle's `Default` namespace. If you changed how the Chronicle integration maps tenants to namespaces, register a matching resolver:

```csharp
builder.AddCratisArc(configureBuilder: arc => arc.WithNeo4j(
    configureNeo4j: neo4j => neo4j.WithNamespaceResolver<MyNamespaceResolver>()));
```

## The database name

The name is `[<prefix>-]<eventStore>[-<namespace>]`. The `Default` namespace adds nothing, so it uses the bare event store name. `GraphDatabaseName.For(prefix, eventStore, namespace)` implements the algorithm:

1. Compose the name. This is the *unsanitized name*.
2. Lower-case it, then replace every character outside `[a-z0-9-]` with `-`.
3. If the result does not start with a letter, prefix it with `g-`.
4. If sanitizing changed the lower-cased name, the name is longer than 63 characters, or it is shorter than 3 characters, truncate it to 54 characters and append `-` and the first 8 lower-case hex characters of the SHA-256 of the unsanitized name as UTF-8.

The hash keeps two different names that sanitize alike, such as `Acme Corp` and `Acme-Corp`, in different databases. With the event store `Orders`:

| Prefix | Namespace | Database |
| --- | --- | --- |
| none | `Default` | `orders` |
| none | `acme` | `orders-acme` |
| `cratis` | `acme` | `cratis-orders-acme` |
| none | `Acme Corp` | `orders-acme-corp-fd6ee09c` |
| none | `Müller` | `orders-m-ller-21f1b1e6` |
| none | `acme.eu` | `orders-acme-eu-550a6c3d` |

An event store `1Orders` in the `Default` namespace is `g-1orders`. A name over 63 characters is cut to 54 and gets the hash, so it is always exactly 63 characters.

:::caution[Lower-casing alone adds no hash]
`Orders`/`Acme` and `Orders`/`acme` select the same database, `orders-acme`. Make namespaces differ by more than case.
:::

## Replace the naming

Implement `IGraphDatabaseNameResolver` if your databases are named differently, and register it:

```csharp
builder.AddCratisArc(configureBuilder: arc => arc.WithNeo4j(
    configureNeo4j: neo4j => neo4j.WithDatabaseNameResolver<MyDatabaseNameResolver>()));
```

A different name addresses a different database. Neo4j answers a read of a database that does not exist with an error, but Chronicle writes the database named by the algorithm above, so only change the naming when you also change what writes it.

## Single-database mode

With `DatabaseMode` set to `Single`, every namespace shares the `SingleDatabase` (`neo4j` by default) and Chronicle stamps the namespace as `_namespace` on every node and relationship. Use it for development and test on a Neo4j edition without multiple databases, never for production: nothing separates the tenants except the filter your query applies.

Filter every pattern using the `$namespace` parameter that the session adds to each query:

```cypher
MATCH (p:Person)-[:MEMBER_OF]->(g:Group)
WHERE p._namespace = $namespace AND g._namespace = $namespace
RETURN p.name AS name, g.name AS group
```

`GraphParameters.Namespace` and `GraphParameters.NamespaceProperty` name the parameter and the property. In `PerNamespace` mode the parameter is still given and harmless, so a query can carry the filter in both modes.
