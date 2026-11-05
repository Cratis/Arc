---
title: "Conditional React queries: wait for arguments"
description: "Use generated when() helpers to wait for query arguments in React without calling hooks conditionally, for ordinary and observable queries."
---

Use `MyQuery.when(condition).use()` to wait for a query argument, such as an account ID chosen by the user, while still calling the hook on every render. A false condition prevents that hook from automatically requesting data or starting an observable subscription.

React hooks cannot go inside an `if` statement. Instead, the generated `when()` method returns a `QueryWhen` or `ObservableQueryWhen` wrapper that passes the condition to the hook as `isEnabled`. These wrappers are classes, not JSX components; call their hook methods in the same place you would call `use()`.

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

The example puts the key on the component that owns the hook, so changing the selection also resets that component's local state. A key is not required to separate query results: ordinary non-Suspense hooks select a cache entry for the new arguments and immediately show its cached result, or a not-ready default if none exists. They do not show the previous account's result under the new ID.

Check the selection before interpreting the result. When disabled, the hook returns `QueryResultWithState.empty(defaultValue)`, which is ready and not performing even though no request ran. Enumerable proxies default to `[]`; single-result proxies default to `{}`, so even `hasData` can be true without loaded data.

When the condition becomes true, an ordinary query fetches, while an observable query joins a matching subscription or starts one. Disabling this hook does not necessarily close an existing shared subscription: another consumer or cache retention can keep it alive. An ordinary non-Suspense hook's explicit `perform()` can also execute while the condition is false. Use `when()` to control automatic execution, not authorization.

## With Paging

Use the same condition with `useWithPaging` when the query returns a collection and the backend supports paging:

```tsx
const [accounts] = AllAccounts.when(isReady).useWithPaging(20, args);
```

## With Suspense

A disabled Suspense hook returns its empty result without suspending. Once enabled, it uses the surrounding loading boundary while waiting for data:

```tsx
// Inside a <Suspense> boundary
const [feed] = LiveFeed.when(!!topicId).useSuspense({ topic: topicId ?? '' });
```

## Observable Queries

For a live feed, keep the hook in place and enable its subscription once the filter has a value. The generated wrapper is `ObservableQueryWhen`:

```tsx
import { LiveFeed } from './Proxies';

export const FilteredFeed = ({ author }: { author: string }) => {
    // Subscription only starts once author is non-empty
    const [feed] = LiveFeed.when(author.length > 0).use({ author });

    return <ul>{feed.data.map(m => <li key={m.id}>{m.text}</li>)}</ul>;
};
```

## How It Interacts with `QueryInstanceCache`

A false condition stops automatic network work, not all local setup. Non-Suspense hooks still create or look up a cache entry and acquire it. Disabled Suspense hooks construct a query to read its default value without starting an enabled resource.

This matters when another component already uses the same query. Observable hooks share subscription updates; ordinary hooks each fetch when enabled. See [query instance caching](./query-instance-caching.md) to decide what a second consumer or a return visit will reuse.

## Memoization

`when()` creates a small wrapper on each render; wrapper identity does not select the query's cache entry. You do not need to memoize it for query reuse. If you choose to retain the wrapper, include every value used by the condition in the dependency array:

```ts
const query = useMemo(() => AllAccounts.when(!!accountId), [accountId]);
const [accounts] = query.use({ id: accountId ?? '' });
```

## `isEnabled` on Raw Hooks

Behind the scenes, `QueryWhen` and `ObservableQueryWhen` pass the condition to the raw `useQuery` / `useObservableQuery` hooks and their `*WithPaging` and `useSuspense*` variants as `isEnabled`. The raw hooks also accept this parameter directly if you need low-level control:

```ts
import { useObservableQuery } from '@cratis/arc.react/queries';

const [result] = useObservableQuery(MyQuery, args, sorting, isEnabled);
```

Use the generated `when()` API for application code so the enabling condition stays beside the query. Continue with [query usage](./usage.mdx) for loading and failure handling, or [Suspense queries](./suspense-queries.md) to move that handling into boundaries.
