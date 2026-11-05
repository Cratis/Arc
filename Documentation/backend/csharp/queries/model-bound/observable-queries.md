---
title: Model-bound observable queries
description: Keep read models current with observable queries in Cratis Arc using MongoDB change streams, explicit transports, and subscription-owned cleanup.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

An observable query in Cratis Arc sends updated read models to a screen as the underlying data changes. The query supplies a producer of values; the client chooses whether to request one snapshot or keep receiving updates through Server-Sent Events (SSE), WebSocket, or a [multiplexed hub subscription](../observable-query-demultiplexer.md). Returning a stream does not itself open a client connection.

## Basic observable query

Configure an Arc host with [the MongoDB provider](../../mongodb/getting-started.md) (`Cratis.Arc.MongoDB`) and a registered collection. MongoDB must support change streams, which requires a replica set or sharded deployment. This observes database state directly and does not require a Chronicle projection.

Replace the earlier `DebitAccount` declaration with this one, using the [shared `AccountId` and `AccountName` concepts](index.md#model-account-identities-and-names). `Observe()` is an extension in the `MongoDB.Driver` namespace. It returns a subject (`ISubject<T>`) that publishes an initial result and later updates. The type-level role protects both query methods; configure authentication so callers can satisfy it.

```csharp
using System.Collections.Generic;
using System.Reactive.Subjects;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using MongoDB.Driver;

namespace Banking.Accounts;

[ReadModel]
[Roles("AccountReader")]
public record DebitAccount(AccountId Id, AccountName Name, decimal Balance)
{
    [Path("/api/accounts/observe")]
    public static ISubject<IEnumerable<DebitAccount>> ObserveAccounts(
        IMongoCollection<DebitAccount> collection,
        decimal minimumBalance = 0) =>
        collection.Observe(account => account.Balance >= minimumBalance);

    [Path("/api/accounts/observe-single")]
    public static ISubject<DebitAccount> ObserveAccount(
        AccountId id,
        IMongoCollection<DebitAccount> collection) =>
        collection.ObserveSingle(account => account.Id == id);
}
```

### When There Is No Matching Document

For this record type, `ObserveSingle()` and `ObserveById()` emit `null` when no document matches. That can happen on the initial query, after deletion, or when an update moves the document out of the filter. It is neither an error nor the end of the subscription: a later matching document can produce another value.

Keep that absence separate from loading or failure in the UI. While `ObserveAccount.use({ id: accountId })` is waiting, show the loading state. Once it is ready, handle denied and unsuccessful results before using `hasData` to decide that no account matches:

```tsx
// ObserveAccount is the generated proxy for the query method above
const [result] = ObserveAccount.use({ id: accountId });

if (!result.isReady) {
    return <Spinner />;
}

if (!result.isAuthorized) return <>Access denied</>;
if (!result.isSuccess) return <>Query failed</>;

if (!result.hasData) {
    return <NotFound />;
}

return <AccountDetails account={result.data} />;
```

See [Observing Collections](../../mongodb/observing-collections.md#when-the-observed-document-is-gone) for the MongoDB-level detail.

A `[ReadModel]` marked `[RemovedWith<T>]` follows the same path when removal hard-deletes its backing document. The active subscriber sees `null`, so the client does not need a separate case for that removal.

The method must still return the declaring read model or a supported wrapper around it. `Task<ISubject<T>>` is allowed; arbitrary `IObservable<T>` is not a model-bound discovery shape. See [return types](return-types.md).

For hub collection subscriptions, each item needs a stable, unique `Id` property to receive change sets (including in `delta` mode). An `Id` concept like `AccountId` in the example works. Without an `Id`, Arc sends the complete collection on every emission without a change set and logs a warning once per subscription. See [collection change streams](../change-stream.md) for transfer modes.

## Custom observable logic

Return the provider's subject directly when it expresses the read. Reactive Extensions (Rx) operators such as `Select`, `CombineLatest`, or `Sample` produce observables rather than subjects. An adapter must do more than forward values: it must preserve [subscription lifetime and terminal errors](#subscription-lifetime) so disconnecting also stops the work upstream.

## Subscription lifetime

Direct transports dispose the subscription they receive, and the hub manages per-subscription resources. Each hub subscription, over WebSocket or SSE, gets its own service scope. That scope is disposed when the subscription ends, so subscription-scoped services remain available while emissions are processed.

When composing a stream, make the disposable returned by `Subscribe` release every upstream subscription. Keep the disposable from `.Subscribe(replaySubject)` and connect it to downstream teardown. Implementing `IDisposable` on a wrapper alone does not establish that connection.

Also forward terminal errors to downstream subscribers and release upstream resources. If the source calls `OnError`, direct SSE and WebSocket transports send a failed query result with the subscription's correlation id before ending, unless already canceled. Exception messages follow the host's `ExposeExceptionDetails` setting; they are not always exposed. The transports can also call the returned subject's `OnError` after interception or delivery failures. An adapter with only an `onNext` handler leaves Rx's default throwing error handler in place, so preserve the error path explicitly.

Test interception failures through both direct transports, terminal error delivery, and repeated subscribe/disconnect cycles. Check that database watchers return to their starting count after disconnecting.

## Paging and failures

MongoDB collection `Observe()` reads the Arc query context and performs [provider-specific paging](paging.md#observable-queries-with-paging). `ObserveSingle()` and `ObserveById()` do not apply query-context paging, sorting, or total-count updates. Arbitrary subjects do not get automatic per-emission slicing either; their producer must implement it.

For the collection and single-document producers above, MongoDB logs observation and change-dispatch failures and forwards them through `OnError` before cleanup. Normal cancellation, disposal, and completion end quietly. Create a new observation after failure rather than reusing the terminated subject.

## Waiting for the first HTTP result

For one MongoDB snapshot, request `waitForFirstResult=true`. Arc subscribes, waits for the first result that emission guards allow, and then disposes the subscription. It returns one result rather than opening a connection for later updates. Denial, an error, or completion can end the wait without a successful result.

The wait defaults to 30 seconds. Set a positive `waitForFirstResultTimeout` in seconds to override it; a suppressed emission does not restart the deadline. A timeout returns 408 and means the wait expired, not that the collection is empty.

Avoid no-wait snapshots for MongoDB `Observe()`. That path neither subscribes nor disposes the subject, and MongoDB's `LifetimeAwareSubject` has no readable `Value` property even after buffering an emission. It therefore returns 202 with `isReady: false`, not a successful empty collection. Because `Observe()` starts its watcher eagerly, the request can also leave that watcher running. Use no-wait snapshots only for producers with a readable current value and verified cleanup without a subscription.

```bash
curl --include 'https://localhost:5001/api/accounts/observe?waitForFirstResult=true&waitForFirstResultTimeout=10'
```

Use a trusted local certificate and your application's normal authentication for this protected example. Each request waits for its own first allowed result, not a change since the previous request. For an open SSE connection or bounded repeated polling, follow the [cURL workflows](../using-observable-queries-with-curl.md).

> [!WARNING]
> Observable HTTP snapshots apply [read-model interception](../read-model-interception.md) before emission guards decide whether to return the data. Interception does not replace query authorization or owner-scoped data selection. Produce only rows the caller may read; use [emission guards](../observable-query-emission-guards.md) when permissions must be checked during delivery.

## Ahead-of-time compilation

The Arc source generator registers the element type of every model-bound query that returns `ISubject<T>` or
`IAsyncEnumerable<T>`, so Arc handles their results with statically typed operations. Element types without a
registration use a cached reflection path under the JIT. When dynamic code is unavailable, such a query fails with
`MissingObservableQueryElementType`, naming the element type. This registration alone does not make the whole
application NativeAOT-compatible.

## Testing observable queries

Use [query scenarios](../../testing/query-scenario.md#observable-queries) to check a subject's first value with `Perform`, or its initial and subsequent emissions with `Observe` and `QueryObservation.Next`. The scenario applies per-emission interception and guards without a hosted transport. MongoDB observation still requires a real MongoDB provider; use hosted integration specs for SSE/WebSocket delivery and database watcher behavior.

## Authorization and frontend integration

[Authorization](authorization.md) runs when subscribing. Use [emission guards](../observable-query-emission-guards.md) when permissions must be rechecked during delivery. Generated proxies and [React observable hooks](../../../../frontend/react/queries/observable-queries.md) manage client subscriptions; reconnecting creates a new subscription, not a durable event-log replay.
