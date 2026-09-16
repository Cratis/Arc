---
title: Observe MongoDB collections
description: Use per-collection change streams with the actual options, lifetime, and failure contract.
---

After ordinary collection reads work, use `Observe()` to publish an initial result and update it from MongoDB change streams. This is a live result set, not an audit log or a guarantee that every intermediate state reaches every subscriber.

## Prerequisites

- Complete [Arc and MongoDB setup](./getting-started.md), including Arc activation. Observation accesses Arc's initialized service provider and query context.
- Use a MongoDB replica set or sharded cluster supporting change streams, with read/change-stream permissions. A standalone server cannot supply the watch.
- Map a document ID to a public CLR property; observation uses it to maintain membership.

## Choose an observation

These are method fragments for a scoped service with an injected `IMongoCollection<Author>` named `collection`. Import `MongoDB.Driver`.

```csharp
var all = collection.Observe();
var active = collection.Observe(author => author.IsActive);
var filter = Builders<Author>.Filter.Eq(author => author.Category, "Fiction");
var fiction = collection.Observe(filter);
var featured = collection.ObserveSingle(author => author.IsFeatured);
var byId = collection.ObserveById<Author, Guid>(authorId);
```

The examples assume your `Author` has a Guid `Id` and the named properties. Collection overloads return `ISubject<IEnumerable<Author>>`; single-document overloads return `ISubject<Author>`. Single observation emits only when an entity exists: it does not emit null when no document matches.

The watch starts when `Observe()` is called, not when the first subscriber arrives. The collection result replays its latest emission to late subscribers. It does not emit a placeholder empty collection before the initial database query finishes.

## Find options, paging, and sorting

`Observe` and `ObserveSingle` accept **nongeneric `FindOptions`**, not `FindOptions<Author>` or projection options:

```csharp
var options = new FindOptions
{
    MaxTime = TimeSpan.FromSeconds(10)
};
var active = collection.Observe(author => author.IsActive, options);
```

This overload does not expose `Sort` or `Limit` properties through `FindOptions`. Arc takes paging and sorting from the current query context. Use the [query paging contract](../queries/model-bound/paging.md) when exposing a paged Arc query; arbitrary controller parameters do not populate that context automatically.

## Lifetime and scope

A directly owned subscription must be disposed. Example lifecycle fragment in an asynchronous method (`System.Reactive.Linq` supplies `Subscribe`):

```csharp
var subject = collection.Observe(author => author.IsActive);
using var subscription = subject.Subscribe(
    authors => Console.WriteLine($"Active authors: {authors.Count()}"),
    () => Console.WriteLine("Author observation completed"));
await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
```

When the last subscriber unsubscribes, the lifetime-aware subject cancels the watch. Cleanup completes/disposes the subject and cursor. Do not create an observation that is never subscribed or retained. Once completed, create a **new** observation rather than reusing the terminated subject.

Arc registers collections as **scoped** services. Register a service that captures `IMongoCollection<T>` as scoped too, not singleton. Keep its scope alive for the subscription's lifetime. A background worker must establish the intended tenant context and resolve a collection inside an appropriate scope; do not cache the first tenant's collection globally. See [MongoDB tenancy](./tenancy.md).

## Errors and recovery limits

The per-collection implementation catches watch failures, logs them, then completes and disposes the subject. It does **not** forward those failures as `OnError`. Individual change-processing exceptions are logged while iteration can continue. Configuration errors before the background watch starts can still throw synchronously.

Consequently, `.Retry(3)` on the existing subject does not recreate a failed watch, and an `OnError` callback is not a reliable connection-failure signal. Monitor completion and logs. If your application needs reconnection, design and test ownership, fresh scopes/subjects, cancellation, and backoff explicitly; this page does not claim a tested automatic retry recipe.

The [shared database watcher](./change-stream-watcher.md) has a different reconnect and error contract. Do not transfer guarantees between the two APIs.

## Change handling and troubleshooting

Insert, update, replace, and delete operations update observed membership. Updates that leave the filter must remove a document from the result; filters are not simply applied to every post-change document at the stream boundary. Do not assume automatic batching of rapid changes.

## When the Observed Document Is Gone

`ObserveSingle()` and `ObserveById()` push a fresh document every time the underlying data changes, but there is no document to push when:

- the observed document is deleted,
- an update or replace moves the document out of the filter (it still exists, but no longer matches), or
- the initial query never found a match in the first place.

In every one of these cases the subject emits `default` — `null` for a reference type — rather than completing. Completing the observable would end the subscription; emitting `null` reports "nothing right now" while leaving the subscription open in case the document reappears, for example if it is re-inserted with the same id, or a later update moves it back into the filter.

> **Note**: A `[ReadModel]` marked `[RemovedWith<T>]` hard-deletes its backing document when the removal event fires. An active `ObserveSingle()`/`ObserveById()` subscriber sees that removal exactly as described above — "the read model was removed" is this same `null` emission, not a special case.

On the frontend, guard against it the same way you guard against "still loading" — check `result.hasData` (and `result.isReady` if you need to distinguish "no result yet" from "ready, but nothing matches"):

```tsx
const [result] = GetAccountObservable.use(accountId);

if (!result.isReady) {
    return <Spinner />;
}

if (!result.hasData) {
    return <NotFound />;
}

return <AccountDetails account={result.data} />;
```

## Advanced Usage

If no first result arrives, verify replica-set configuration, permissions, ID mapping, and connectivity. Enable logging for `MongoDB.Driver.MongoCollection` to inspect per-collection watch failures. Test filtered membership, deletion, paging, and forced disconnections in your deployment before relying on live results.
