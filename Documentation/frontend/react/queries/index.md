# Queries

Queries retrieve data from the backend. They are generated from C# controller actions and provide
type-safe, strongly-typed access to your backend data.

## Overview

Queries come in two flavors:

- **Regular queries** — Request/response HTTP GET calls. Use these when you want to fetch data once or on demand.
- **Observable queries** — Server-push subscriptions that keep your UI in sync with backend state in real time.

Both are generated automatically from your C# backend by the [proxy generator](../../../backend/proxy-generation/index.md).

## HTTP Headers

Queries automatically include HTTP headers provided by the `httpHeadersCallback` configured in [Arc](../arc.md).
This enables authentication cookies, authorization tokens, or other custom headers to be sent with every
query request without per-query configuration.

## Proxy Generation

The [proxy generator](../../../backend/proxy-generation/index.md) scans your backend HTTP GET controller actions
and produces TypeScript classes that:

- Match your backend query structure
- Provide type-safe result types
- Offer static `.use()` and `.useWithPaging()` methods for React integration
- Support the `when(condition)` conditional pattern

See [Proxy Generation](../proxy-generation.md) for setup details.

## Quick Start

**Backend query (C#):**

```csharp
[HttpGet]
public IEnumerable<DebitAccount> AllAccounts() => _collection.Find(FilterDefinition<DebitAccount>.Empty).ToList();
```

**Generated TypeScript:**

```typescript
import { AllAccounts } from './generated/queries';

export const AccountList = () => {
    const [result] = AllAccounts.use();

    return (
        <ul>
            {result.data.map(account => <li key={account.id}>{account.name}</li>)}
        </ul>
    );
};
```

## Query Result

All query hooks return a `QueryResultWithState<T>` which extends `QueryResult` with React-specific state:

| Property | Description |
| -------- | ----------- |
| `data` | The actual data returned, strongly typed. |
| `isSuccess` | Whether the query succeeded. |
| `isAuthorized` | Whether the caller was authorized. |
| `isValid` | Whether the request was valid. |
| `validationResults` | Any validation errors. |
| `hasExceptions` | Whether the server threw an exception. |
| `exceptionMessages` | Exception messages if any. |
| `exceptionStackTrace` | Exception stack trace if any. |
| `paging` | Paging metadata (current page, page size, total items, total pages). |
| `hasData` | Shorthand — `true` when `data` is non-empty. |
| `isPerforming` | `true` while the query is in-flight. |

## Topics

| Topic | Description |
|-------|-------------|
| [React Usage](./react-usage.md) | Using the `.use()` hook, paging, sorting, and observable query transport in React. |
| [Query Scope](./scope.md) | Tracking performing state across multiple queries in composite UIs and ViewModels. |

## Related Documentation

- [Suspense Queries](../suspense-queries.md) — Suspense and ErrorBoundary integration.
- [Conditional Queries](../conditional-queries.md) — The `when(condition)` pattern for optional queries.
- [Observable Query Multiplexing](../observable-query-multiplexing.md) — Transport selection and connection pooling.
- [Query Instance Caching](../query-instance-caching.md) — Shared query instances across components.
- [Change Stream](../change-stream.md) — Fine-grained delta streaming.
- [Commands](../commands/index.md) — Sending data back to the backend.
