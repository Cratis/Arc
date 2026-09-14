---
title: Controller query arguments
description: Bind MVC route values, query strings, collections, and validated DTOs.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## MVC binding is a separate contract

Controller GET actions use ASP.NET Core model binding, not Arc's scalar model-bound query readers. Use `[FromRoute]`, `[FromQuery]`, and `[FromHeader]` to make each source explicit. MVC can bind arrays and DTO properties from query strings; this does not imply a static `[ReadModel]` method supports those same HTTP input shapes.

## Bind and validate a search DTO

This alternative banking example uses the [shared `AccountId` and `AccountName` concepts](../model-bound/index.md#model-account-identities-and-names) and an ASP.NET Core Arc host with MVC, authorization, and the MongoDB provider configured. `Prefix` is search text—not a complete account name—so it remains a string at the request boundary:

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Banking.Accounts;

public record DebitAccount(AccountId Id, AccountName Name, decimal Balance);

public class AccountSearch
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Prefix { get; set; } = string.Empty;

    [Range(0, 1000000)]
    public decimal MinimumBalance { get; set; }
}

[Authorize(Roles = "AccountReader")]
[Route("api/accounts")]
public class AccountsController(IMongoCollection<DebitAccount> collection) : ControllerBase
{
    [HttpGet("search")]
    public IEnumerable<DebitAccount> Search([FromQuery] AccountSearch query)
    {
        var prefix = new BsonRegularExpression("^" + Regex.Escape(query.Prefix));
        var filter = Builders<DebitAccount>.Filter.Regex(account => account.Name, prefix) &
            Builders<DebitAccount>.Filter.Gte(account => account.Balance, query.MinimumBalance);
        return collection.Find(filter).SortBy(account => account.Name).Limit(100).ToList();
    }

    [HttpGet("by-ids")]
    public IEnumerable<DebitAccount> ByIds([FromQuery] Guid[] ids)
    {
        var accountIds = ids.Select(id => (AccountId)id);
        return collection.Find(Builders<DebitAccount>.Filter.In(account => account.Id, accountIds)).ToList();
    }
}
```

```http
GET /api/accounts/search?prefix=Sav&minimumBalance=100
GET /api/accounts/by-ids?ids=11111111-1111-1111-1111-111111111111&ids=22222222-2222-2222-2222-222222222222
```

The GUID array is an explicit MVC wire boundary; the action converts it to domain `AccountId` values before querying. The prefix filter targets the stored account-name field and escapes the caller's search text rather than interpreting it as a regular expression.

The validation attributes in this example belong to **MVC DTO properties**. Arc's GET action filter consults MVC model state before invoking the action. `[ApiController]` and custom MVC filters can add their own earlier validation responses; do not assume all configurations return the identical envelope.

A filter on caller-supplied IDs is not owner authorization. This example deliberately requires a role allowed to read all matching accounts.

## Defaults, sorting, and paging

Nullable/defaulted scalar action arguments support optional input according to MVC's binding rules. For Arc paging, prefer returning `IQueryable<T>` and using [the paging context](paging.md) instead of slicing twice.

If composing a MongoDB fluent query yourself, use expression-based sorting such as `SortBy(account => account.Name)`, or a `Builders<T>.Sort` definition passed to `Sort(...)`. There is no `SortBy(string)` overload. Constrain client-selectable sort fields to your intended public fields.

## Request body arguments

An ordinary MVC action can use `[FromBody]` and a suitable verb for structured JSON. That is **not** a drop-in Arc GET observable query: `QueryActionFilter` runs for GET, and the standard Arc POST command surface has a different contract. Do not put `[HttpPost]` on a subject-returning action and assume Arc will stream it.

For model-bound one-shot queries needing a body, see [HTTP QUERY](../using-the-http-query-method.md); its body envelope still uses scalar argument conversion, not MVC DTO deserialization. Choose and test the endpoint/client contract explicitly rather than treating these paths as interchangeable.

Continue with [route templates](route-templates.md) for path parameters and [validation](../validation.md) for the model-bound/MVC distinction.
