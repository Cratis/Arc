---
title: Cratis Arc tenant database isolation
description: Cratis Arc names MongoDB databases by tenant; EF Core needs an isolation design, and the Chronicle integration maps tenants to namespaces.
---

Cratis Arc's tenant context selects separate storage only when the integration uses it. MongoDB adds a tenant suffix by default, EF Core keeps the configured connection, and the optional Chronicle integration selects an event-store namespace.

Before accessing that storage, verify that the caller may use the selected tenant. Register an application-owned [`ITenantMembership` check](./membership.md) or enforce membership at a trusted ingress. For a request-to-storage walkthrough of all three integrations, see [tenancy end to end](./end-to-end.md).

## MongoDB database naming

With the optional Arc MongoDB integration, `DefaultMongoDatabaseNameResolver` uses:

```text
{BaseDatabaseName}             // TenantId.NotSet or TenantId.Default
{BaseDatabaseName}+{TenantId}  // any other tenant
```

For a base name `MyDatabase`, both `TenantId.NotSet` and `TenantId.Default` select `MyDatabase`; tenant `acme-corp` selects `MyDatabase+acme-corp`. `TenantId.IsDefault` covers both default aliases. Preserve that behavior in custom resolvers unless you intentionally migrate to different naming.

To place tenants on other servers or databases, or give them their own credentials, use [connection resolution](./connection-resolution.md) instead of a custom name resolver.

The suffix selects a database; it does not check caller membership or grant database permissions. Reject unauthorized tenant selections before accessing tenant-scoped services. Resolve collections within the intended tenant's scope rather than retaining them in a singleton, where later requests could reuse the wrong destination. See [MongoDB tenancy](../mongodb/tenancy.md).

## Neo4j database naming

With the optional [Arc Neo4j integration](../neo4j/tenancy.md), `DefaultGraphDatabaseNameResolver` names the graph database from the configured event store and the tenant's namespace, using the algorithm Chronicle's Neo4j graph sink uses to choose the database it writes:

```text
{EventStore}                  // TenantId.NotSet or TenantId.Default
{EventStore}-{TenantId}       // any other tenant, lower-cased and sanitized
```

Names are lower-cased, sanitized to `[a-z0-9-]`, and a name that sanitizing changed or that is too long gets a hash suffix. The same note applies as for MongoDB: the name selects a database; it does not check caller membership. Place tenants elsewhere with [Neo4j connection resolution](../neo4j/connection-resolution.md).

## Entity Framework Core database naming

Arc's EF Core registration uses the configured connection string, including in pooled context registration. It does **not** automatically append the tenant ID or create a database per tenant. To connect each tenant to its own database, server or identity, configure [connection resolution](./connection-resolution.md); Arc then resolves the connection for the current tenant every time it opens.

Choose relational isolation to match your application: separate connections or databases, schemas, or enforced tenant filtering in shared tables. Check every read and write path, including pooled-context reuse. Reading a tenant ID alone does not make an ordinary `DbContext` tenant-aware, and connection resolution does not add tenant filters. See [EF Core configuration](../entity-framework/automatic-database-hookup.md) for registration.

## Chronicle event store namespace

Enable the optional [Chronicle integration](../chronicle/tenancy.md) when the application uses Chronicle. Its namespace resolver maps a named tenant to a namespace with the same name; both `TenantId.NotSet` and `TenantId.Default` select Chronicle's `Default` namespace.

The Chronicle kernel controls the physical storage behind that namespace. Arc's MongoDB and EF Core connection settings do not configure the kernel, so align both destinations if the application reads read models that Chronicle writes.

## Verify the boundary

Test an unset tenant, the named default, and two named tenants. Include concurrent requests, reused scopes or pools, caches and background work. Check the actual storage destination as well as the tenant ID: a correctly resolved ID cannot repair a service instance that still points at another tenant's data. Verify membership denials separately from destination selection.
