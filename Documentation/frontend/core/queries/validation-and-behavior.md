---
title: Validation and behavior
description: Validate query arguments and understand paging, cancellation, readiness, and frontend query failures.
---

Core queries validate input and provide predictable execution behavior.

## Client-side validation

Validation metadata is generated from backend FluentValidation rules through the proxy generator.

```typescript
const query = new SearchUsersQuery();
query.parameters = { searchTerm: 'ab', minAge: -5 };

const result = await query.perform();
// result.isValid === false
// result.validationResults contains validation errors
```

Observable queries validate the same way. `perform()` returns an invalid result, and `subscribe()` delivers one to
your callback instead of opening a connection:

```typescript
const query = new ObserveUsers();

query.subscribe(result => {
    // result.isValid === false when the arguments were rejected,
    // which is distinct from a valid result that simply has no data yet
}, { minAge: -5 });
```

Because `subscribe()` validates the arguments it is given, a subscription started before its arguments are available
is rejected rather than left open. Gate it with `ObservableQueryWhen` so the subscription only starts once the
arguments exist:

In a React component, use the generated wrapper (illustrative fragment):

```tsx
const [books] = BooksForAuthor.when(!!authorId).use({ authorId });
```

`ObservableQueryWhen` is a class returned by `.when()`, not a JSX component. Supply a correctly typed argument/default even while disabled; the condition suppresses automatic subscription, not all query/cache creation. See [conditional queries](../../react/queries/conditional-queries.md).

Client-side validation is a convenience, not a gate — every rule it applies is also enforced by the server, so
calling an endpoint directly gains nothing. Server rejections report member names the same way the client does:
camelCased, and attributed to the field rather than to a concept's inner value.

For general validation docs, see [Validation](../validation/index.md).

## Sorting and paging

Queries include native sorting and paging primitives via `Sorting` and `Paging`.

For React usage patterns and generated hooks, see [Paging](../../react/queries/paging.md).

## Request cancellation

When a newer request supersedes an active one, Arc cancels stale work to reduce race conditions and unnecessary processing.

## Error categories

Typical query failure categories include:

- Parameter validation errors
- Network failures
- Timeouts and cancellations
- Server exceptions

## See also

- [Query Contracts](./contracts.md)
- [React Queries](../../react/queries/index.md)
