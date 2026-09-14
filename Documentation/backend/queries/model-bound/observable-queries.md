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

The producer loads initial data asynchronously, watches changes, and emits updated state. `ObserveSingle` emits only when it has a matching document; an absent document is not an automatic null emission or 404. Do not use silence to infer deletion. If the client must observe disappearance, observe a filtered collection so an empty set can express it.

The method must still return the declaring read model or a supported wrapper around it. `Task<ISubject<T>>` is allowed; arbitrary `IObservable<T>` is not a model-bound discovery shape. See [return types](return-types.md).

## Custom observable logic

Return the provider subject directly when it expresses the read. Rx operators such as `Select`, `CombineLatest`, or `Sample` produce observables; adapting them to Arc's subject contract must preserve both [subscription lifetime and terminal errors](#subscription-lifetime). This keeps derived streams safe to disconnect, not just able to forward values.

## Subscription lifetime

Direct transports dispose the subscription they receive; the hub also manages per-subscription resources. A composition must therefore honor two contracts:

- **Subscription-owned teardown:** the disposable returned by `Subscribe` must release every upstream subscription. Keep the disposable from `.Subscribe(replaySubject)`; implementing `IDisposable` on a wrapper alone does not connect it to downstream teardown.
- **Terminal error propagation:** deliver errors to downstream subscribers and release upstream resources. Direct SSE and WebSocket transports can call the returned subject's `OnError` after interception or delivery failures. An onNext-only adapter uses a default throwing error handler; explicitly preserve the terminal-error path rather than swallowing it.

Test interception failures through both direct transports, terminal error delivery, and repeated subscribe/disconnect cycles. Verify watcher counts return to baseline.

## Paging and failures

MongoDB `Observe()` reads the Arc query context and performs [provider-specific paging](paging.md#observable-queries-with-paging). Arbitrary subjects do not get automatic per-emission slicing.

For the collection `Observe()` and `ObserveSingle()` producers above, MongoDB watcher failures are **logged and followed by completion/disposal**, not forwarded through `OnError`. Per-change processing failures are logged separately and the watcher may continue. Therefore `Do(onError: ...)` cannot guarantee notification of these source failures. Downstream Rx operator errors are a separate error channel. Monitor provider logs and stream health; completion does not prove that the data is current.

## Waiting for the first HTTP result

For MongoDB snapshots, use `waitForFirstResult=true` so Arc subscribes, receives the first emission, and disposes that subscription. Waiting defaults to 30 seconds; a positive `waitForFirstResultTimeout` overrides it in seconds, and timeout returns 408.

A no-wait snapshot neither subscribes nor disposes the subject. MongoDB's `LifetimeAwareSubject` has no readable `Value` property, even after buffering an emission, so this request returns 202 with `isReady: false`. Because `Observe()` starts its watcher eagerly, it can leave that watcher running. Reserve no-wait snapshots for producers with a readable current value and verified cleanup on the no-subscription path.

```bash
curl --include 'https://localhost:5001/api/accounts/observe?waitForFirstResult=true&waitForFirstResultTimeout=10'
```

Use a trusted local certificate and your application's normal authentication for this protected example. This request waits for a first emission, **not a change since the previous request**. See [cURL workflows](../using-observable-queries-with-curl.md) for SSE and bounded repeated polling.

> [!WARNING]
> Observable HTTP snapshots currently bypass [read-model interception](../read-model-interception.md). Produce only data the caller is allowed to receive; do not depend on streaming-only masking to protect a snapshot endpoint.

## Authorization and frontend integration

[Authorization](authorization.md) runs when subscribing. Use [emission guards](../observable-query-emission-guards.md) when permissions must be rechecked during delivery. Generated proxies and [React observable hooks](../../../frontend/react/queries/observable-queries.md) manage client subscriptions; reconnecting creates a new subscription, not a durable event-log replay.
