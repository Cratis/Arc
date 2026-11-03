---
title: Model-bound observable queries
description: Watch database state with subscription-owned streams and explicit transport choices.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

A page that should stay current needs a producer of updates, not repeated manual refreshes. An observable query exposes that producer through Arc. The client chooses a snapshot, direct SSE/WebSocket, or a [multiplexed hub subscription](../observable-query-demultiplexer.md); a return type alone does not establish a connection.

## Basic observable query

This alternative declaration uses the [shared `AccountId` and `AccountName` concepts](index.md#model-account-identities-and-names), an Arc host configured with **Cratis.Arc.MongoDB**, a registered collection, and MongoDB change-stream support (a replica set or sharded deployment). `Observe()` lives in the `MongoDB.Driver` namespace. This is ordinary database observation in standalone Arc, not a Chronicle projection.

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

`ObserveSingle()` and `ObserveById()` emit `null` — not an error, and not a completed observable — when there is no document to report: the document was deleted, an update moved it out of the filter, or the initial query never found one. The subscription stays open, so if a document with the same key reappears later, subscribers start receiving it again.

Check the result before rendering absence. While `ObserveAccount.use({ id: accountId })` is waiting, show the loading state. Once it is ready, handle denied and unsuccessful results before using `hasData` to decide that no account matches. A successful null value leaves the subscription open for a later matching document:

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

This is also what happens when a `[ReadModel]` marked `[RemovedWith<T>]` is removed: removal hard-deletes the backing document, and an active subscriber sees that removal as this same `null` emission — "the read model was removed" is not a special case to handle separately.

The method must still return the declaring read model or a supported wrapper around it. `Task<ISubject<T>>` is allowed; arbitrary `IObservable<T>` is not a model-bound discovery shape. See [return types](return-types.md).

For hub collection subscriptions, each item needs a stable, unique `Id` property to receive change sets (including in `delta` mode). An `Id` concept like `AccountId` in the example works. Without an `Id`, Arc sends the complete collection on every emission without a change set and logs a warning once per subscription. See [collection change streams](../change-stream.md) for transfer modes.

## Custom observable logic

Return the provider subject directly when it expresses the read. Rx operators such as `Select`, `CombineLatest`, or `Sample` produce observables; adapting them to Arc's subject contract must preserve both [subscription lifetime and terminal errors](#subscription-lifetime). This keeps derived streams safe to disconnect, not just able to forward values.

## Subscription lifetime

Direct transports dispose the subscription they receive; the hub also manages per-subscription resources. Each hub subscription (WebSocket and SSE) gets its own service scope when it starts, and the scope is disposed when the subscription ends, so scoped and transient services resolved for a subscription behave as they do for an HTTP request. A composition must therefore honor two contracts:

- **Subscription-owned teardown:** the disposable returned by `Subscribe` must release every upstream subscription. Keep the disposable from `.Subscribe(replaySubject)`; implementing `IDisposable` on a wrapper alone does not connect it to downstream teardown.
- **Terminal error propagation:** deliver errors to downstream subscribers and release upstream resources. Direct SSE and WebSocket transports send a failed query result with the subscription's correlation id and exception message (without a stack trace) before ending a stream whose source calls `OnError`, unless the subscription was already canceled. Direct SSE and WebSocket transports can call the returned subject's `OnError` after interception or delivery failures. An onNext-only adapter uses a default throwing error handler; explicitly preserve the terminal-error path rather than swallowing it.

Test interception failures through both direct transports, terminal error delivery, and repeated subscribe/disconnect cycles. Verify watcher counts return to baseline.

## Paging and failures

MongoDB `Observe()` reads the Arc query context and performs [provider-specific paging](paging.md#observable-queries-with-paging). Arbitrary subjects do not get automatic per-emission slicing.

For the collection `Observe()` and `ObserveSingle()` producers above, MongoDB logs observation and change-dispatch failures and forwards them to subscribers through `OnError` before cleaning up the observation. Normal cancellation, disposal, and completion end quietly, without an error notification. After a failed observation, create a new observation rather than reusing the terminated subject.

## Waiting for the first HTTP result

For a MongoDB snapshot, use `waitForFirstResult=true`: Arc subscribes and waits for the first result that emission guards allow, then disposes the subscription. A suppressed emission does not restart the deadline. This returns one result rather than opening a connection for later updates; denial, an error, or completion can end the wait without a successful result. Waiting defaults to 30 seconds; a positive `waitForFirstResultTimeout` overrides it in seconds, and timeout returns 408. A timeout means the wait expired, not that the collection is empty.

A no-wait snapshot neither subscribes nor disposes the subject. MongoDB's `LifetimeAwareSubject` has no readable `Value` property, even after buffering an emission, so this request returns 202 with `isReady: false`. That is no current result, not a successful empty collection. Because `Observe()` starts its watcher eagerly, it can leave that watcher running. Reserve no-wait snapshots for producers with a readable current value and verified cleanup on the no-subscription path.

```bash
curl --include 'https://localhost:5001/api/accounts/observe?waitForFirstResult=true&waitForFirstResultTimeout=10'
```

Use a trusted local certificate and your application's normal authentication for this protected example. This request waits for a first allowed result, **not a change since the previous request**. See [cURL workflows](../using-observable-queries-with-curl.md) for SSE and bounded repeated polling.

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
