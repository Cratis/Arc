# Change Stream

Observable collection queries in Arc deliver full snapshots on every update by default. As collections grow, shipping the entire collection on every MongoDB change stream event becomes expensive. The **change stream** feature reduces this overhead by computing a delta — which items were added, replaced, or removed — and attaching it to each `QueryResult` as a `ChangeSet`.

## How It Works

When the `ObservableQueryDemultiplexer` receives a new snapshot from a `ISubject<IEnumerable<T>>`, it compares the snapshot with the previous one using the `ChangeSetComputor` and populates `QueryResult.ChangeSet` with the delta. The full snapshot is still available in `QueryResult.Data` — the `ChangeSet` is additive, not a replacement.

### Identity-Based Delta (Recommended)

When the item type exposes a property conventionally named `Id` (case-insensitive), the computor uses it to build a precise three-way diff:

| Operation | Condition |
|-----------|-----------|
| `Added`   | An item with a new `Id` value appears in the current snapshot. |
| `Replaced`| An item with the same `Id` exists in both snapshots but its JSON representation differs. |
| `Removed` | An item with an `Id` present in the previous snapshot is absent from the current snapshot. |

### JSON-Hash Fallback

When no `Id` property is found, the computor serializes each item to JSON and uses the full JSON as a hash key. This surfaces `Added` and `Removed` items but **cannot detect `Replaced`** (because item identity is unknown).

## Wire Format

The `ChangeSet` is serialized as part of `QueryResult` and sent over the WebSocket or SSE connection alongside the full `Data` field:

```json
{
  "data": [ /* full current snapshot */ ],
  "changeSet": {
    "added":    [ /* new items */      ],
    "replaced": [ /* updated items */  ],
    "removed":  [ /* deleted items */  ]
  }
}
```

When no `ChangeSet` is present on a `QueryResult`, the client must treat `Data` as the full current snapshot.

## `ChangeSet` Type

```csharp
public class ChangeSet
{
    public IEnumerable<object> Added    { get; set; } = [];
    public IEnumerable<object> Replaced { get; set; } = [];
    public IEnumerable<object> Removed  { get; set; } = [];
}
```

## `ChangeSetComputor`

The `ChangeSetComputor` class is responsible for delta computation and can be used independently:

```csharp
var computor = new ChangeSetComputor(serializerOptions);

// First call — all items are Added
ChangeSet initial = computor.Compute(null, currentItems);

// Subsequent calls — computes the delta
ChangeSet delta = computor.Compute(previousItems, currentItems);
```

### Identity Property Discovery

`ChangeSetComputor.FindIdentityProperty(type)` locates the identity property by looking for a property named `Id` (case-insensitive). This static helper can be used in tests or custom infrastructure:

```csharp
PropertyInfo? idProp = ChangeSetComputor.FindIdentityProperty(typeof(MyReadModel));
```

## See Also

- [Change Stream — Frontend](../../frontend/react/change-stream.md) — React `useChangeStream()` hook, transfer mode configuration, and usage examples.
- [Observable Query Demultiplexer](observable-query-demultiplexer.md) — The backend component that manages WebSocket and SSE connections and invokes the `ChangeSetComputor`.
- [Observable Query Hub](observable-query-hub.md) — Wire protocol reference for observable query connections.
