---
title: Controller query route templates
description: Define explicit MVC routes and bind resource identifiers from the path.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Bind a route value

MVC combines the controller's `[Route]` with the action's `[HttpGet]` template. Unlike model-bound `[Path]` queries, MVC reads values from route placeholders.

This alternative banking example uses the [shared domain concepts](../model-bound/index.md#model-account-identities-and-names) and `[AspNetResult]` so its not-found response has ordinary MVC 404 semantics instead of Arc's null-result failure. It requires an ASP.NET Core Arc host, configured authorization, and the MongoDB provider.

```csharp
using System;
using System.Threading.Tasks;
using Cratis.Arc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Banking.Accounts;

public record DebitAccount(AccountId Id, AccountName Name, decimal Balance);

[Authorize(Roles = "AccountReader")]
[Route("api/accounts")]
public class AccountsController(IMongoCollection<DebitAccount> collection) : ControllerBase
{
    [AspNetResult]
    [HttpGet("{id:guid}", Name = "AccountById")]
    public async Task<ActionResult<DebitAccount>> ById([FromRoute] Guid id)
    {
        AccountId accountId = id;
        var account = await collection.Find(candidate => candidate.Id == accountId).FirstOrDefaultAsync();
        if (account is null)
        {
            return NotFound();
        }
        return Ok(account);
    }
}
```

`GET /api/accounts/11111111-1111-1111-1111-111111111111` binds a GUID at the HTTP boundary. The action converts it to `AccountId` before the domain-specific query. The action returns a raw account or 404, not a `QueryResult`. Use a client expecting that raw contract; see [without wrappers](../../asp-net-core/without-wrappers.md).

## Template reference

These are **route-template fragments**, not independent controller declarations:

| Template                                               | Use                                                                   |
| ------------------------------------------------------ | --------------------------------------------------------------------- |
| `api/accounts` on the controller, `{id}` on the action | A resource under the controller path                                  |
| `{id:guid}`                                            | Match only GUID-shaped route values                                   |
| `{id:int}`                                             | Match only integer-shaped route values                                |
| `category/{category?}`                                 | Optional final segment; give the action argument a compatible default |
| `~/api/account-summary`                                | Override rather than append to the controller path                    |
| `api/[controller]`                                     | Substitute the MVC controller name                                    |
| `[action]`                                             | Substitute the action name                                            |

Constraints choose which endpoint matches; they are not a replacement for business validation or authorization. A nested path such as `/customers/{customerId}/accounts/{accountId}` does not prove ownership. Check the authenticated caller's access and apply both identifiers in the actual database predicate.

## Choose route or query string

Use route values for resource identity and query strings for filters. Keep the predicate consistent with the declared inputs: do not accept a category/date/filter and then return an unfiltered collection.

For the simpler static-query alternative, use [model-bound arguments and explicit paths](../model-bound/query-arguments.md#url-binding). For MVC collection/DTO binding, continue with [query arguments](query-arguments.md).
