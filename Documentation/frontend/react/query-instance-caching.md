# Query Instance Caching

Arc maintains an application-scoped cache of query instances. When two components subscribe to the same query with the same arguments, they share a single underlying query object rather than creating two independent instances — reducing server round-trips and keeping all consumers in sync automatically.

## How It Works

The cache is keyed by **query type name** and **serialized arguments**. The following rules apply:

| Scenario | Result |
|----------|--------|
| Same type + same arguments | Same shared instance |
| Same type + different arguments | Different instances |
| Two components, same type + same args | One shared instance; both receive updates simultaneously |

When a new subscriber mounts, it receives the **last known result** immediately from the cache rather than waiting for the next server push. This means components that unmount and remount (or re-render into a new subtree) present stale-but-fast data first, then update as fresh data arrives.

When the last subscriber unmounts, the cache entry is released. The next time any component subscribes to the same query, a fresh connection is established.

## Lifecycle

```
Component A mounts
  → Cache miss → create new query instance → subscribe to server → receive first result
  → Cache stores the result

Component B mounts (same query, same args)
  → Cache hit → reuse existing instance
  → Immediately seeded with the last known result from the cache
  → No new server connection

Component A unmounts
  → Release reference (ref-count decrements)
  → Instance stays alive because Component B still holds a reference

Component B unmounts
  → Release reference → ref-count reaches 0 → cache entry evicted
  → Query instance unsubscribed from server
```

## Transparent Integration

The cache is used automatically by `useObservableQuery` and `useQuery`. No changes are needed in component code — the public hook signatures are unchanged.

```tsx
// These two components share one query instance and one server connection.
export const BalanceSummary = () => {
    const [result] = AllAccounts.use();
    return <span>Total: {result.data?.length ?? 0} accounts</span>;
};

export const AccountList = () => {
    const [result] = AllAccounts.use();
    return <DataTable value={result.data} />;
};
```

Both `BalanceSummary` and `AccountList` consume the same `AllAccounts` instance. When `AllAccounts` pushes an update, both components re-render simultaneously.

## Cache Context

The cache is provided via `QueryInstanceCacheContext`, initialized once per `<Arc>` mount. This means the cache scope matches the application boundary — all components under `<Arc>` share the same cache, and there is no need to configure anything explicitly.

```tsx
// The cache is initialized here, once per app.
<Arc microservice="my-app">
    <MyApp />
</Arc>
```

## Parameterized Queries

The cache key includes serialized arguments, so queries with different arguments are stored as separate instances.

```tsx
// These are TWO separate cache entries — different arguments.
const [accountsForAlice] = AccountsByOwner.use({ owner: 'alice' });
const [accountsForBob]   = AccountsByOwner.use({ owner: 'bob' });

// This is the SAME cache entry as accountsForAlice above.
const [aliceAgain] = AccountsByOwner.use({ owner: 'alice' });
```

Arguments are serialized by sorting the object keys alphabetically and stringifying, so `{ a: 1, b: 2 }` and `{ b: 2, a: 1 }` produce the same cache key.

## Relationship with the Conditional `when()` Hook

The `when(condition)` pattern interacts with the cache correctly: when `isEnabled` is `false`, no cache lookup or creation occurs, and no server connection is established. The hook returns `QueryResultWithState.empty()` without touching the cache.

```tsx
// No cache entry is created until `selectedId` is truthy.
const [result] = AllProjects.when(!!selectedId).use({ id: selectedId });
```

Once the condition becomes `true`, the cache is looked up or populated on the next render.

## See also

- [Observable Query Multiplexing](./observable-query-multiplexing.md) — How hub connections are managed and configured.
- [Queries](./queries.md) — General query hooks and the `when()` conditional pattern.
- [Observable Query Hub](../../backend/queries/observable-query-hub.md) — Server-side protocol and authorization.
