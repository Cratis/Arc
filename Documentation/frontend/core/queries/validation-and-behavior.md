# Validation And Behavior

Core queries validate input and provide predictable execution behavior.

## Client-Side Validation

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

Client-side validation is a convenience, not a gate — every rule it applies is also enforced by the server, so
calling an endpoint directly gains nothing.

For general validation docs, see [Validation](../validation/index.md).

## Sorting And Paging

Queries include native sorting and paging primitives via `Sorting` and `Paging`.

For React usage patterns and generated hooks, see [Paging](../../react/queries/paging.md).

## Request Cancellation

When a newer request supersedes an active one, Arc cancels stale work to reduce race conditions and unnecessary processing.

## Error Categories

Typical query failure categories include:

- Parameter validation errors
- Network failures
- Timeouts and cancellations
- Server exceptions

## See Also

- [Query Contracts](./contracts.md)
- [React Queries](../../react/queries/index.md)
