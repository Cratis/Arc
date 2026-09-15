# Conditional Queries

Sometimes a component receives a query argument through a prop or local state that may not be available yet — for example, an entity ID that starts as `undefined` until the user makes a selection. Issuing the query before the value is available leads to an invalid request. At the same time, React's rules of hooks prohibit calling hooks inside `if` statements, so you cannot simply wrap `use()` in a conditional.

The `when()` pattern solves this. Every generated query class exposes a static `when(condition)` method that returns a `QueryWhen` (or `ObservableQueryWhen`) wrapper. The wrapper exposes hook methods that forward the condition as `isEnabled`. When false, it suppresses automatic requests/subscriptions; it does not suppress all instance creation or cache access. Use paging only for enumerable results with backend paging support. `QueryWhen` and `ObservableQueryWhen` are classes, not JSX components.

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

export const AccountDetail = ({ accountId }: { accountId: string | undefined }) => (
    <AccountLoader key={accountId} accountId={accountId} />
);

const AccountLoader = ({ accountId }: { accountId: string | undefined }) => {
    // Hook always runs — no conditional hook violation.
    // When accountId is undefined the query is disabled.
    const [accounts] = AllAccounts.when(!!accountId).use({ id: accountId ?? '' });

    if (!accountId) return <p>Select an account.</p>;
    if (!accounts.isReady || accounts.isPerforming) return <p>Loading…</p>;
    if (!accounts.isSuccess) return <p>Could not load accounts.</p>;

    return <div>{accounts.data.map(a => <p key={a.id}>{a.name}</p>)}</div>;
};
```

The key belongs on the component that owns the query hook. When the selection changes from one nonempty ID to another, React remounts that loader with the new arguments. Without this boundary, the current ordinary non-Suspense hook can retain the previous selection's ready result while the next request runs; `isReady` and `isPerforming` alone do not establish which ID produced the data. The keyed loader prevents that prior selection from being displayed as the new one. A cached result for the **same** ID may still appear while it revalidates; this is not a freshness or authorization guarantee.

When `condition` is `false`:

- This hook does not automatically start a request or subscription. An existing shared subscription may remain active through another consumer or cache retention.
- The hook returns `QueryResultWithState.empty(defaultValue)` with `isPerforming: false`. Enumerable defaults are `[]`; single-result proxies default to `{}`, which makes `hasData` true. Neither `hasData` nor an empty result proves the query ran.
- For ordinary non-Suspense hooks, an explicit `perform()` can still execute: the condition is not an authorization boundary.

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

Non-Suspense hooks create/look up and acquire an entry before checking the condition in their effects. Disabled Suspense hooks also construct a query to obtain its default value, but do not start an enabled Suspense resource. Instance allocation is therefore not prevented by `when(false)`. Observable cache listeners and ordinary query fetching have different sharing behavior; see [query instance caching](./query-instance-caching.md).

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
