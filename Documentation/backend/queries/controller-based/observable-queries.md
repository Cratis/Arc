---
title: Controller-based observable queries
description: Expose a MongoDB-backed producer through MVC GET streaming.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Declare a GET action

Return a subject from an MVC GET action to make observable handling available. The client/request chooses a snapshot, SSE, or WebSocket; returning `ISubject<T>` does not itself open a connection.

This alternative banking declaration uses the [shared domain concepts](../model-bound/index.md#model-account-identities-and-names), an ASP.NET Core Arc host configured with **Cratis.Arc.MongoDB**, MVC authorization, a registered collection, and MongoDB change-stream support:

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

The filter is applied by the database observer. This is standalone Arc database observation, not a Chronicle projection. `ObserveSingle` does not emit a null value when no document matches; use an observed collection when an empty result must represent disappearance.

## Keep MVC and model-bound handling distinct

Arc's `QueryActionFilter` handles **GET** actions. Do not change the example to `[HttpPost]` with `[FromBody]` and assume the same streaming adapter runs. Arbitrary `IObservable<T>` values are not enough either: runtime streaming detection recognizes subjects and async-enumerables.

MVC authorization protects the controller endpoint. The hub resolves queries by performer name through the query pipeline; a controller route URL is not automatically a hub query name. Use a discovered [model-bound query](../model-bound/observable-queries.md) for the documented [hub subscription protocol](../observable-query-demultiplexer.md), rather than assuming MVC action filters protect a separate hub subscription.

## Compose without leaking

Return `collection.Observe()` directly when it expresses the read. MVC uses the same [subscription lifetime and terminal-error contract](../model-bound/observable-queries.md#subscription-lifetime): disposing the returned subscription must release upstream resources, and terminal errors must reach subscribers. Follow that shared checklist when composing streams, including both direct transports' error and teardown tests.

MongoDB `Observe()` handles paging through its query context. A plain subject does not automatically slice emissions; see [observable paging](paging.md#observable-paging).

## Errors and snapshots

Monitor provider logs and stream health: these MongoDB producers log watcher failures and complete/dispose rather than forwarding them through `OnError`. See [provider failures](../model-bound/observable-queries.md#paging-and-failures) for the distinction from per-change and downstream operator errors.

For a one-shot MongoDB read, use `waitForFirstResult=true`. A no-wait GET returns 202 without subscribing or disposing the subject and can retain an eagerly started watcher. The shared [snapshot guidance](../model-bound/observable-queries.md#waiting-for-the-first-http-result) explains current-value requirements, cleanup, and timeouts. See [cURL workflows](../using-observable-queries-with-curl.md) for requests you can run.

> [!WARNING]
> Observable HTTP snapshots currently bypass [read-model interception](../read-model-interception.md). Keep unauthorized fields and rows out of the producer's output rather than relying only on streaming interception to mask them.

## Ongoing authorization

An initial authorized subscription does not automatically end when permissions change. Use [emission guards](../observable-query-emission-guards.md) for per-emission revocation checks and test disconnect cleanup. The [React observable-query APIs](../../../frontend/react/queries/observable-queries.md) manage client subscription lifecycle; reconnecting establishes a new read, not durable replay.
