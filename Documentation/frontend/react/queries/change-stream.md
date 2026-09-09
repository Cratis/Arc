---
title: Change stream
description: Consume snapshot changes from enumerable observable queries, with exact generated overloads and Full/Delta limitations.
---

A live list consumer sometimes needs additions, replacements, and removals instead of the latest full array. `useChangeStream` exposes those changes as React state. It is a snapshot-delta view, **not an audit log or an operation-by-operation database stream**. Arc does not require MongoDB or Chronicle for it.

## Generated proxy method

Only enumerable observable queries generate `useChangeStream`. Argument positions depend on whether the backend query has parameters. These are illustrative calls inside components consuming the named generated proxies:

```tsx
// Parameterless query: first argument is the key accessor.
const allChanges = AllBooks.useChangeStream(book => String(book.id));

// Parameterized query: first argument is the parameters object.
const authorChanges = BooksForAuthor.useChangeStream(
    { authorId }, book => String(book.id)
);
```

For a parameterized query whose arguments are optional, `useChangeStream(undefined, getKey)` is valid. It is not the signature for a parameterless proxy. Use a stable primitive key; newly deserialized Guid objects are not stable JavaScript Map keys, so convert them to strings.

## useChangeStream hook

The raw hook takes `(query, args?, getKey?, sorting?, isEnabled = true)` and returns `ChangeSet<TItem>`. It reuses ordinary observable subscription/cache infrastructure. It does not add a separate transport connection for the same shared subscription.

| Member | Meaning |
| --- | --- |
| `added` | Newly present items |
| `replaced` | Current items whose stable keys match but serialized content changed |
| `removed` | Previously present items no longer in the snapshot |

For local comparison, `getKey` enables replacement detection. Without it, full JSON content identifies items, so edits appear as removal plus addition. Reordering alone is not an item-content change. The return value is the latest change state, not a queue of every intermediate event.

Disabling the hook suppresses its automatic subscription/change processing, not all cache creation; see [conditional queries](./conditional-queries.md).

## Transfer modes

Processing follows this order:

1. If a result includes a server `changeSet`, use it directly, **even in local Full mode**.
2. Otherwise, the first processed snapshot or local `ObservableQueryTransferMode.Full` returns all current items as `added` and empty replacement/removal arrays.
3. Otherwise, local Delta mode compares previous and current snapshots.

Configure the mode through `<Arc observableQueryTransferMode={ObservableQueryTransferMode.Full}>`, importing the enum from `@cratis/arc`, before establishing subscriptions. This global setting also travels in shared-hub subscription requests: the server sends full snapshots in Full mode, or an initial snapshot followed by delta-only collection updates in Delta mode. It is not isolated by nested providers, and it does not select SSE versus WebSocket or guarantee operation-level transport fidelity.

The ordinary observable `.use()` hook reconstructs delta-only collections; [observable Suspense currently does not](./suspense-queries.md#observable-collections-and-delta-only-updates).

The server's `ChangeSetComputor` also compares snapshots. Intermediate writes between emissions can disappear from the observed delta. Equivalent cached data may suppress notifications, so Full mode does not guarantee one callback per server tick. Use an actual durable event/audit source when every operation matters.

## IChangeStreamFor

`IChangeStreamFor<TItem>` extends `IObservableQueryFor<TItem[]>` and is the structural contract used by the raw hook. Generated enumerable observable proxies satisfy it. No separate marker attribute or MongoDB operation feed is required.

## See also

- [Backend change streams](../../../backend/queries/change-stream.md)
- [Observable query multiplexing](./observable-query-multiplexing.md)
- [Query instance caching](./query-instance-caching.md)
