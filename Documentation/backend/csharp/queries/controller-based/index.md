---
title: Build controller-based queries with Cratis Arc
description: Return Cratis Arc query results from ASP.NET Core MVC GET actions while using MVC routing, argument binding, and authorization policies.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

A Cratis Arc controller-based query lets you read data through an ASP.NET Core MVC GET action and return Arc query results. Choose this approach when you want MVC routing, model binding, or authorization policies. The query method lives on the controller, separate from the returned type; a [model-bound query](../model-bound/index.md) keeps the method on the read model instead.

## Start with an MVC GET action

The action below reads all accounts from a MongoDB collection and requires the `AccountReader` role. It uses the [shared `AccountId` and `AccountName` concepts](../model-bound/index.md#model-account-identities-and-names) with an ASP.NET Core Arc host, MVC, and the configured Arc MongoDB provider. Use this `DebitAccount` declaration instead of the model-bound alternative. No Chronicle integration is required.

```csharp
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Banking.Accounts;

public record DebitAccount(AccountId Id, AccountName Name, decimal Balance);

[Authorize(Roles = "AccountReader")]
[Route("api/accounts")]
public class AccountsController(IMongoCollection<DebitAccount> collection) : ControllerBase
{
    [HttpGet]
    public IEnumerable<DebitAccount> AllAccounts() => collection.Find(_ => true).ToList();
}
```

With authentication and authorization middleware configured, `[Authorize]` protects the action before it reads the collection. MVC binds the request arguments. Arc's `QueryActionFilter` establishes the query context and checks MVC model state before allowing the action to run. The returned data passes through read-model interception and appears inside a [QueryResult](../query-pipeline.md#query-result-metadata) response with query metadata.

This MVC path uses an action filter, not the model-bound `IQueryPipeline` filter chain. Custom model-bound query filters do not run here. The Arc wrapper described above handles GET actions, so changing the verb to POST does not preserve that contract.

## Choose the next step

- [Route templates](route-templates.md) bind identifiers from route values.
- [Query arguments](query-arguments.md) accept scalar values, arrays, and MVC request objects.
- [Dependency injection](dependency-injection.md) supplies storage and application services.
- [Return types](return-types.md) describe wrapping, nulls, and explicit MVC responses.
- [Paging](paging.md) lets clients navigate an `IQueryable<T>` result.
- [Observable queries](observable-queries.md) keep clients updated through server-sent events or WebSocket.

After choosing the action's arguments and return type, use the [proxy generator](../../proxy-generation/index.md) to create client types from supported controller query declarations. If you need raw MVC responses, follow [without wrappers](../../asp-net-core/without-wrappers.md) and provide a client that understands that response shape. A standard Arc query client expects the Arc wrapper.
