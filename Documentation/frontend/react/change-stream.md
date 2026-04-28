# Change Stream

Observable queries deliver full collection snapshots by default. **Change stream** is a thin layer on top that exposes per-update deltas — which items were added, replaced, or removed — as React state instead of requiring consumers to diff the full array themselves.

## Quick Start

```tsx
// All.useChangeStream() is auto-generated for any enumerable observable query.
const changes = All.useChangeStream(
    undefined,           // optional query arguments
    item => item.id      // optional key accessor — enables "replaced" detection
);

useEffect(() => {
    changes.added.forEach(item => console.log('added', item));
    changes.replaced.forEach(item => console.log('replaced', item));
    changes.removed.forEach(item => console.log('removed', item));
}, [changes]);
```

## `useChangeStream()` Hook

```typescript
function useChangeStream<TDataType, TQuery, TArguments>(
    query:     Constructor<TQuery>,
    args?:     TArguments,
    getKey?:   (item: TDataType) => unknown,
    sorting?:  Sorting,
    isEnabled: boolean = true
): ChangeSet<TDataType>
```

The hook uses the same subscription and caching infrastructure as `useObservableQuery` — no extra server connections are opened.

### Parameters

| Parameter   | Type | Description |
|-------------|------|-------------|
| `query`     | `Constructor<TQuery>` | The observable query constructor (must be enumerable). |
| `args`      | `TArguments` | Optional query arguments passed through to the underlying query. |
| `getKey`    | `(item: TDataType) => unknown` | Optional identity accessor. When provided, items with the same key but different content are reported as `replaced`. Without it, only `added` and `removed` are detected. |
| `sorting`   | `Sorting` | Optional sorting configuration. |
| `isEnabled` | `boolean` | When `false`, the hook is a no-op. Defaults to `true`. |

### Return Value

A `ChangeSet<TDataType>` object that is stable between updates when nothing changes:

```typescript
interface ChangeSet<T> {
    readonly added:    T[];   // items that appeared since the last update
    readonly replaced: T[];   // items that have the same key but different content
    readonly removed:  T[];   // items that were present before but are not now
}
```

## Generated Proxy Method

The proxy generator emits a `static useChangeStream()` method on every enumerable observable query class automatically. No manual wiring is needed:

```typescript
// Generated proxy — call site
import { All } from './Proxy';

const changes = All.useChangeStream(undefined, item => item.id);
```

## Transfer Modes

Two transfer modes control how the client processes incoming updates.

### Delta mode (default)

When the server attaches a `ChangeSet` to the `QueryResult`, the hook uses it directly. This is the most accurate mode because the server tracks individual MongoDB operations (insert, update, delete) rather than diffing snapshots.

When no server `ChangeSet` is available, the hook falls back to client-side snapshot comparison:

- **With `getKey`**: added, replaced, and removed are all detected.
- **Without `getKey`**: only added and removed are detected (identity unknown).

### Full mode

Set `observableQueryTransferMode` to `ObservableQueryTransferMode.Full` on the `<Arc>` component to treat every incoming snapshot as if all its items were just added. This is useful for debugging or for consumers that need a fresh batch on every tick regardless of what actually changed.

```tsx
import { Arc } from '@cratis/arc.react';
import { ObservableQueryTransferMode } from '@cratis/arc';

export const App = () => (
    <Arc
        microservice="my-app"
        observableQueryTransferMode={ObservableQueryTransferMode.Full}
    >
        <MyRoutes />
    </Arc>
);
```

| Value | Description |
|-------|-------------|
| `ObservableQueryTransferMode.Delta` | Default. Uses server-provided `ChangeSet` or falls back to client-side snapshot comparison. |
| `ObservableQueryTransferMode.Full`  | Treats every snapshot as a fresh batch of additions. |

## `IChangeStreamFor<T>`

All generated enumerable observable query classes automatically implement `IChangeStreamFor<TItem>` (which extends `IObservableQueryFor<TItem[]>`). You can use this interface in generic components:

```typescript
import { IChangeStreamFor } from '@cratis/arc/queries';

function MyComponent<T, TQuery extends IChangeStreamFor<T>>({
    query
}: { query: Constructor<TQuery> }) {
    const changes = useChangeStream<T, TQuery>(query);
    // ...
}
```

## See Also

- [Change Stream — Backend](../../backend/queries/change-stream.md) — `ChangeSetComputor`, identity property discovery, and wire format.
- [Observable Query Multiplexing](./observable-query-multiplexing.md) — Transport configuration (WebSocket vs SSE, connection count, direct mode).
- [Queries](./queries/index.md) — General query hooks and usage patterns.
