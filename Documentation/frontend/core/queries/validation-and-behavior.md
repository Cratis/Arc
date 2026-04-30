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
