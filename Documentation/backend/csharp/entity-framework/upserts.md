---
title: Upserts
description: Choose tracked persistence or an immediate, atomic provider-native upsert.
---

`Cratis.Arc.EntityFrameworkCore` provides two upsert APIs on `DbSet<TEntity>`.
Both match the complete primary key configured in the EF model.

## Tracked upserts

`await context.Items.Upsert(item)` keeps the existing tracked behavior: it finds an
existing entity, copies values into its tracked instance, or marks a new entity as
added. Call `SaveChangesAsync` to persist it. This is a read followed by a write,
not an atomic database upsert; concurrent inserts can still conflict.

## Native upserts

`await context.Items.UpsertAsync(item, cancellationToken)` executes one parameterized
SQL statement on supported relational mappings:

| Provider | Statement |
| -------- | --------- |
| SQL Server | `MERGE ... WITH (HOLDLOCK)` |
| PostgreSQL | `INSERT ... ON CONFLICT ... DO UPDATE` |
| SQLite | `INSERT ... ON CONFLICT ... DO UPDATE` |

The primary-key match and write are atomic, including concurrent inserts of the
same key. Retrying the same values updates that row rather than adding another.
A key-only entity uses a no-op on conflict instead of an empty update clause.
Deadlocks, unrelated unique constraints, and other database errors still propagate;
this API does not introduce an automatic retry policy.

Table, schema, column, and key names come from EF metadata and are quoted by the
provider. Values are parameters using EF's relational type mappings, including
configured value converters. Null values remain null. Native execution uses the
context's connection and current transaction and honors the cancellation token.

### Change tracking and fallback

Native execution is immediate: **do not call `SaveChangesAsync` to persist the
upsert**. It does not attach the supplied entity, reconcile already tracked
instances, or refresh store-generated properties. Reload previously tracked
instances before relying on their values. Pending tracked changes are not flushed.

Use application-assigned keys and single-table scalar mappings for native
execution. Other providers, unset generated keys, SQL Server identity columns,
owned or complex properties, inheritance, and multi-table mappings fall back to
the tracked upsert and **require `SaveChangesAsync`**. Properties whose generated
value is still unset also use the tracked fallback so EF can apply database defaults.
The fallback does not provide the native path's concurrency guarantee. Choose the
tracked `Upsert` explicitly when consistent deferred persistence is required.
