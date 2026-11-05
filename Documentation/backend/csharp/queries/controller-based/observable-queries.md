---
title: Stream Cratis Arc controller query updates
description: Return a MongoDB-backed subject from a Cratis Arc MVC GET action to deliver account updates over server-sent events, WebSocket, or an HTTP snapshot.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

An observable controller query lets clients receive updated data when the underlying MongoDB collection changes. Return an `ISubject<T>`, a reactive source that publishes values to subscribers, from an MVC GET action. The request selects server-sent events (SSE), WebSocket, or a one-shot HTTP snapshot; the return type alone does not open a connection.

## Declare a GET action

`ObserveAccounts()` publishes accounts that meet the caller's minimum balance. `ObserveAccount()` watches one account by ID. Both use the [shared domain concepts](../model-bound/index.md#model-account-identities-and-names) and require an ASP.NET Core Arc host configured with `Cratis.Arc.MongoDB`, MVC authorization, a registered collection, and MongoDB change-stream support. Use this `DebitAccount` declaration instead of the model-bound alternative:

```csharp
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Banking.Accounts;

public record DebitAccount(AccountId Id, AccountName Name, decimal Balance);

[Authorize(Roles = "AccountReader")]
[Route("api/accounts")]
public class AccountsController(IMongoCollection<DebitAccount> collection) : ControllerBase
{
    [HttpGet("observe")]
    public ISubject<IEnumerable<DebitAccount>> ObserveAccounts(
        [FromQuery] decimal minimumBalance = 0) =>
        collection.Observe(account => account.Balance >= minimumBalance);

    [HttpGet("{id:guid}/observe")]
    public ISubject<DebitAccount> ObserveAccount([FromRoute] Guid id)
    {
        AccountId accountId = id;
        return collection.ObserveSingle(account => account.Id == accountId);
    }
}
```

The database observer applies each filter and publishes the matching data. This reads MongoDB directly and does not require a Chronicle projection. `ObserveSingle` emits `null` when no document matches, whether that happens initially, after a delete, or after an update moves the document out of the filter. The subscription stays open so a later match can still arrive. Handle `null` as "not found" on the client, as with [model-bound observable queries](../model-bound/observable-queries.md).

## Keep MVC and model-bound handling distinct

Arc's `QueryActionFilter` recognizes streaming results from GET actions. Changing the action to `[HttpPost]` with `[FromBody]` bypasses that adapter. Runtime detection recognizes subjects and async-enumerables, so an arbitrary `IObservable<T>` implementation is not enough to select streaming handling.

MVC authorization protects the controller route. The hub instead resolves a query by performer name through the query pipeline, where MVC action filters do not run. A controller route URL is not automatically a hub query name. For the documented hub workflow, use a discovered [model-bound query](../model-bound/observable-queries.md) and follow the [hub subscription protocol](../observable-query-demultiplexer.md).

## Compose without leaking

Return `collection.Observe()` directly when it expresses the read. If you compose it with reactive operators or another producer, preserve the [subscription lifetime and terminal-error contract](../model-bound/observable-queries.md#subscription-lifetime). Disposing the returned subscription must release upstream resources, and terminal errors must reach subscribers. Use that shared checklist to test errors and teardown over both direct transports.

MongoDB `Observe()` reads paging from the query context. A plain subject does not automatically slice its emissions, so check [observable paging](paging.md#observable-paging) before returning a custom collection-producing stream.

## Errors and snapshots

When Arc's action filter rejects invalid query arguments, a direct WebSocket or SSE request receives one `QueryResult` containing `isValid: false`, validation results, and the correlation ID before the connection closes. A plain HTTP request receives HTTP 400 with the query result. MVC filters that respond earlier can produce a different validation response.

MongoDB observation failures reach subscribers through `OnError`, and the producer cleans up its resources. Check provider logs as well as client error handling when diagnosing a stopped stream. The shared [provider failure guidance](../model-bound/observable-queries.md#paging-and-failures) covers observation and downstream operator failures.

For a one-shot MongoDB read, use `waitForFirstResult=true` to wait for data. Without it, a GET can return the subject's current value if one is available, or HTTP 202 if it is not ready. The no-wait path does not subscribe to or dispose the subject, so it can retain an eagerly started watcher. Follow the shared [snapshot guidance](../model-bound/observable-queries.md#waiting-for-the-first-http-result) for current-value requirements, cleanup, and timeouts.

Controller HTTP snapshots pass data through [read-model interception](../read-model-interception.md) and emission guards before returning it. Keep authorization filtering in the producer where possible, and apply the same access rules to snapshots and streaming requests. Use the [cURL workflows](../using-observable-queries-with-curl.md) to exercise the transport you intend to expose.

## Ongoing authorization

An authorized connection does not automatically end when permissions change. Add [emission guards](../observable-query-emission-guards.md) to check revocation before delivering each value, and test cleanup when the connection ends.

When connecting a React screen, use the [observable-query APIs](../../../../frontend/react/queries/observable-queries.md) to manage the client subscription lifecycle. Reconnecting establishes a new read; it does not replay a durable history of missed updates.
