# Query Configuration

Queries can be configured for service routing and endpoint behavior in both React and non-React environments.

## Microservice Routing

Per query:

```typescript
query.setMicroservice('inventory-service');
```

In React applications, configure this globally with `<Arc microservice="..." />`.

## API Base Path

Per query:

```typescript
query.setApiBasePath('/api/v1');
```

In React applications, set this globally with `<Arc apiBasePath="..." />`.

## Observable Query Transport (`queryDirectMode`)

By default, observable queries use centralized hub endpoints. Set `Globals.queryDirectMode = true` to bypass the hub and connect directly per query.

```typescript
import { Globals } from '@cratis/arc';

Globals.queryDirectMode = true;
```

| Value | Description |
|-------|-------------|
| `false` (default) | Route through `/.cratis/queries/sse` or `/.cratis/queries/ws`. |
| `true` | Connect directly to each observable query WebSocket URL. |

In React, this is usually configured through `<Arc queryDirectMode={...} />`. See [React query integration](../../react/queries/index.md).

## See Also

- [Observable Queries](../../react/queries/observable-queries.md)
- [Observable Query Multiplexing](../../react/queries/observable-query-multiplexing.md)
