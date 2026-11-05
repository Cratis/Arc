---
title: Bind arguments for Cratis Arc controller queries
description: Use ASP.NET Core MVC binding to accept route values, query strings, arrays, and validated request objects in Cratis Arc controller GET actions.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

ASP.NET Core MVC binds request values to your controller GET action's arguments. Use explicit binding attributes when you need to accept an account ID from the route, search criteria from the query string, or a value from a header.

## MVC binding is a separate contract

`[FromRoute]`, `[FromQuery]`, and `[FromHeader]` identify where MVC should read each value. MVC can also bind repeated query-string keys to an array and bind query-string fields to a request data transfer object (DTO).

These examples use MVC's binding path. Arc-generated query endpoints use Arc's argument readers and the query pipeline, even when the discovered query is a controller method. Their conversion rules are a separate contract; the MVC DTO example below does not establish support for the same request shape on a generated endpoint or a static `[ReadModel]` method.

## Bind and validate a search DTO

`AccountSearch` groups the search fields and their validation rules into one request object. `Prefix` remains a string because it contains part of an account name, not a complete domain `AccountName`. The example uses the [shared `AccountId` and `AccountName` concepts](../model-bound/index.md#model-account-identities-and-names) and an ASP.NET Core Arc host with MVC, authorization, and the MongoDB provider configured. Use this `DebitAccount` declaration instead of the model-bound alternative:

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

The first request binds `prefix` and `minimumBalance` to `AccountSearch`. The action escapes the prefix before building the regular expression, so caller text is treated literally when matching the start of the stored account name. The second request binds repeated `ids` keys to a GUID array. `ByIds()` converts those GUIDs to domain `AccountId` values before querying.

MVC validates the DTO properties using `[Required]`, `[StringLength]`, and `[Range]`. Arc's GET action filter checks MVC model state before invoking the action and rejects invalid input. `[ApiController]` or custom MVC filters can respond earlier, so their validation response may differ from Arc's wrapper.

Both actions require the `AccountReader` role because they can read any matching account. Filtering by caller-supplied IDs does not establish that the caller owns those accounts.

## Defaults, sorting, and paging

For optional MVC input, use nullable scalar arguments or give them defaults according to MVC's binding rules. Keep those rules separate from Arc-generated controller query endpoints: on that path, invalid supplied scalars, concepts, and collection elements produce HTTP 400 with a malformed-request validation error naming the argument and its type, for both GET and QUERY. The action does not run. Supplying an invalid nullable or defaulted concept value is still an error; it is not treated as missing input.

To page the result, return `IQueryable<T>` and let [the paging context](paging.md) drive Arc's renderer. Avoid slicing in the action as well, which would page the result twice.

If you compose a MongoDB fluent query yourself, use expression-based sorting such as `SortBy(account => account.Name)`, or pass a `Builders<T>.Sort` definition to `Sort(...)`. There is no `SortBy(string)` overload. Restrict client-selectable sort fields to the fields you intend to expose.

## Request body arguments

An ordinary MVC action can accept structured JSON with `[FromBody]` and a suitable HTTP verb. Arc's `QueryActionFilter` handles GET, however, so changing a subject-returning action to `[HttpPost]` does not retain Arc's streaming behavior. The standard Arc POST command surface also has a different contract.

For a model-bound one-shot query that needs a body, follow [HTTP QUERY](../using-the-http-query-method.md). That body envelope uses Arc's argument conversion, not MVC DTO deserialization. Choose the endpoint and client together, then test the request shape they actually use.

If an identifier belongs in the URL path, continue with [route templates](route-templates.md). Use [validation](../validation.md) to compare MVC model-state handling with validation on the model-bound path.
