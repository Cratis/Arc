---
title: Choose tracked or native upserts in Cratis Arc
description: Use Cratis Arc's EF Core upserts to defer writes until SaveChangesAsync or execute a native SQL upsert immediately on supported mappings.
---

An upsert inserts a row if its key is new or updates the row with that key.
Cratis Arc's `Cratis.Arc.EntityFrameworkCore` package offers tracked upserts that
wait for `SaveChangesAsync` and native upserts that execute immediately on supported
mappings. Both APIs extend `DbSet<TEntity>` and match the complete primary key
configured in the EF model.

## Tracked upserts

Choose `Upsert` when the write should wait for `SaveChangesAsync` alongside your
other tracked changes. After `await context.Items.Upsert(item)`, the change is
prepared in the context, not yet written to the database.

The lookup checks locally tracked entities first, then queries the database if
needed. If it finds an existing entity, it copies the supplied values into that
tracked instance; otherwise, it marks the supplied entity as added. Call
`SaveChangesAsync` to persist the change. The lookup and later write are separate
operations, so concurrent inserts can still conflict.

## Native upserts

Choose `UpsertAsync` when you need the database to match the key and write the row
in one atomic statement. On supported relational mappings,
`await context.Items.UpsertAsync(item, cancellationToken)` executes the provider's
SQL immediately, without waiting for `SaveChangesAsync`. Check the
[tracking and fallback rules](#change-tracking-and-fallback) before relying on this
behavior: unsupported providers or mappings use tracked persistence instead.

The native path executes one parameterized statement:

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

After a native upsert completes, the SQL has already executed; `SaveChangesAsync`
is not needed to persist that upsert. Pending tracked changes are not flushed.
Native execution does not attach the supplied entity, reconcile already tracked
instances, or refresh store-generated properties. An entity you loaded earlier
can therefore still hold its old values. Reload previously tracked instances
before relying on their values.

Use application-assigned keys and single-table scalar mappings for native
execution. Other providers, unset generated keys, SQL Server identity columns,
navigations (including owned and many-to-many relationships), shadow or complex
properties, inheritance, and multi-table mappings fall back to
the tracked upsert and **require `SaveChangesAsync`**. Properties whose generated
value is still unset also use the tracked fallback so EF can apply database defaults.
The fallback does not provide the native path's concurrency guarantee. Choose the
tracked `Upsert` explicitly when consistent deferred persistence is required.

If you need to configure the keys or properties used by an upsert, continue with
[entity mapping](./entity-mapping.md).
