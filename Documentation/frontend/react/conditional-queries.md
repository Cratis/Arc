# Conditional Queries

Sometimes a component receives a query argument through a prop or local state that may not be available yet — for example, an entity ID that starts as `undefined` until the user makes a selection. Issuing the query before the value is available leads to an invalid request. At the same time, React's rules of hooks prohibit calling hooks inside `if` statements, so you cannot simply wrap `use()` in a conditional.

The `when()` pattern solves this. Every generated query class exposes a static `when(condition)` method that returns a `QueryWhen` (or `ObservableQueryWhen`) wrapper. The wrapper exposes the same `use`, `useWithPaging`, `useSuspense`, and `useSuspenseWithPaging` methods as the query class, but the underlying hook is a no-op when `condition` is `false`. When `condition` is `true`, behaviour is identical to calling the hook directly.

## Generated API

The proxy generator emits a `when()` static method on every query and observable query class:

```ts
// Generated for a regular query
static when(condition: boolean): QueryWhen<AllAccounts, Account[], AllAccountsParameters>

// Generated for an observable query
static when(condition: boolean): ObservableQueryWhen<LiveFeed, Message[], LiveFeedParameters>
```

## Basic Usage

```tsx
import { AllAccounts } from './Proxies';

export const AccountDetail = ({ accountId }: { accountId: string | undefined }) => {
    // Hook always runs — no conditional hook violation.
    // When accountId is undefined the query is disabled.
    const [accounts] = AllAccounts.when(!!accountId).use({ id: accountId ?? '' });

    if (!accounts.hasData) return <p>Select an account.</p>;

    return <div>{accounts.data.map(a => <p key={a.id}>{a.name}</p>)}</div>;
};
```

When `condition` is `false`:

- No HTTP request or WebSocket/SSE subscription is created.
- The hook returns `QueryResultWithState.empty()` — `hasData` is `false`, `isPerforming` is `false`, `data` is `[]` / `undefined` depending on the query shape.

When `condition` becomes `true` on a subsequent render, the hook connects to the server and the component re-renders with real data.

## With Paging

```tsx
const [accounts] = AllAccounts.when(isReady).useWithPaging(20, args);
```

## With Suspense

```tsx
// Inside a <Suspense> boundary
const [feed] = LiveFeed.when(!!topicId).useSuspense({ topic: topicId ?? '' });
```

## Observable Queries

The pattern works identically for observable queries via `ObservableQueryWhen`:

```tsx
import { LiveFeed } from './Proxies';

export const FilteredFeed = ({ author }: { author: string }) => {
    // Subscription only starts once author is non-empty
    const [feed] = LiveFeed.when(author.length > 0).use({ author });

    return <ul>{feed.data.map(m => <li key={m.id}>{m.text}</li>)}</ul>;
};
```

## How It Interacts with `QueryInstanceCache`

When `condition` is `false` no cache entry is created or referenced. The moment `condition` turns `true` the hook resolves or creates a cache entry for the type + arguments combination. If another component has already populated that cache entry, the new subscriber receives the last known result immediately — no server round-trip needed. See [Query Instance Caching](./query-instance-caching.md) for details.

## Memoization

`when()` creates a lightweight wrapper object on every render. For components where the condition expression is complex or the render is performance-sensitive, you can memoize the result:

```ts
const query = useMemo(() => AllAccounts.when(!!accountId), [accountId]);
const [accounts] = query.use({ id: accountId ?? '' });
```

In practice the object is small enough that memoization is rarely needed.

## `isEnabled` on Raw Hooks

Behind the scenes `QueryWhen` and `ObservableQueryWhen` delegate to the raw `useQuery` / `useObservableQuery` (and their `*WithPaging` and `useSuspense*` variants) by passing `isEnabled` as the last argument. The raw hooks also accept this parameter directly if you need low-level control:

```ts
import { useObservableQuery } from '@cratis/arc.react/queries';

const [result] = useObservableQuery(MyQuery, args, sorting, isEnabled);
```

The generated `when()` API is preferred for application code because it keeps intent explicit and co-located with the query class.
