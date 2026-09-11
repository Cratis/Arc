---
title: Controller-based queries
description: Use Arc query results with ASP.NET Core MVC routing and binding.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Use a controller when your query needs MVC routing, model binding, or authorization policies. Unlike a [model-bound query](../model-bound/index.md), the method does not need to live on the type it returns.

## Start with an MVC GET action

This controller example uses the [shared `AccountId` and `AccountName` concepts](../model-bound/index.md#model-account-identities-and-names) with an ASP.NET Core Arc host, MVC, and the configured Arc MongoDB provider. Use this `DebitAccount` declaration instead of the model-bound alternative. No Chronicle integration is required.

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

With authentication and authorization middleware configured, the MVC authorization attribute protects this action. Arc's `QueryActionFilter` handles **GET** actions: it establishes query context, processes MVC model-state errors, renders the returned data, applies interception, and wraps it in [QueryResult](../query-pipeline.md#query-result-metadata).

This is not the model-bound `IQueryPipeline` filter chain. Do not assume model-bound custom filters run for a controller action, or that Arc's GET query wrapper applies to a POST action.

## Choose the next step

- [Route templates](route-templates.md): bind identifiers from route values.
- [Query arguments](query-arguments.md): bind scalar values, arrays, and MVC DTOs.
- [Dependency injection](dependency-injection.md): resolve application services.
- [Return types](return-types.md): understand wrapping, nulls, and explicit MVC responses.
- [Paging](paging.md): return `IQueryable<T>` for renderer-based paging.
- [Observable queries](observable-queries.md): expose a producer over direct SSE or WebSocket.

The [proxy generator](../../proxy-generation/index.md) creates client types from supported controller query declarations. For raw MVC responses, see [without wrappers](../../asp-net-core/without-wrappers.md); opting out changes the contract a standard Arc query client expects.
