---
title: Model-bound query arguments
description: Scalar HTTP binding, optional arguments, reserved keys, and explicit paths.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Bind a scalar argument

Arc's model-bound HTTP readers are not ASP.NET MVC model binding. GET reads named values from the **query string**, not route values. QUERY reads an arguments envelope, converts each value to a string, and uses the same scalar conversion path.

This alternative read-model declaration uses the [shared `AccountId` and `AccountName` concepts](index.md#model-account-identities-and-names) and the configured Arc MongoDB provider. The query searches by an exact account name and a minimum balance:

```csharp
using System.Linq;
using Cratis.Arc.Queries.ModelBound;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Banking.Accounts;

[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, decimal Balance)
{
    [Path("/api/accounts/search")]
    public static IQueryable<DebitAccount> Search(
        IMongoCollection<DebitAccount> collection,
        AccountName name,
        decimal minimumBalance = 0) =>
        collection.AsQueryable().Where(account =>
            account.Name == name && account.Balance >= minimumBalance);
}
```

```http
GET /api/accounts/search?name=Savings&minimumBalance=100
```

Arc converts the incoming name to `AccountName`; the predicate uses that domain value and the requested minimum balance. This is query filtering, **not authorization**: never trust an owner ID supplied by the caller without checking that caller's access.

## Supported input shapes

The built-in conversion path handles scalar values such as strings, numbers, booleans, GUIDs, enums, dates, and supported `ConceptAs<T>` wrappers. A custom `TypeConverter` can extend conversion; test it through each transport you expose.

It does **not** provide general nested-JSON DTO binding or array/list deserialization. A JSON object in `arguments` does not make an arbitrary `SearchCriteria` parameter bindable. Repeated GET keys likewise are not a promise of collection binding. Unsupported conversion can yield a missing/null/default value or a conversion error, rather than a useful DTO.

These limitations concern the supplied HTTP readers. Already-typed arguments passed directly to `IQueryPipeline`, custom readers/converters, and [MVC DTO binding](../controller-based/query-arguments.md) are different paths. FluentValidation's ability to traverse an object does not prove HTTP can construct that object.

## Missing, empty, and optional values

Argument names match case-insensitively. GET and QUERY readers skip empty string values. At invocation, absent optional arguments use the method's default value. Nullable value types can be omitted; nonnullable value types without defaults are required. Concept nullability/defaults affect requiredness.

Plain reference types, including `string`, are implicitly optional in the current performer even without a nullable annotation. **A `string` parameter alone is not a required-input rule.** Use a whole-argument [query validator](../validation.md) to reject missing or empty input. Put required dependencies before optional arguments, as in the example above.

## Reserved keys

The GET reader removes these names from ordinary arguments, case-insensitively:

| Keys                                              | Purpose                          |
| ------------------------------------------------- | -------------------------------- |
| `page`, `pageSize`                                | Arc paging context               |
| `sortby`, `sortDirection`                         | Arc sorting context              |
| `waitForFirstResult`, `waitForFirstResultTimeout` | Observable HTTP snapshot control |

Do not declare method parameters named `page` or `pageSize` expecting GET to populate them. Use [automatic paging](paging.md), or distinct business argument names if you implement a separate result cap. QUERY places paging/sorting in separate envelope properties; hub subscriptions also have dedicated paging/sorting fields. Keep the distinction explicit across transports.

## URL binding

Without `[Path]`, Arc derives a kebab-case path from the namespace and generated-API options, including the configured route prefix, skipped namespace segments, and query-name inclusion. It does not infer `/{id}` from a parameter named `id` or a method named `ById`.

Use `Cratis.Arc.Queries.ModelBound.PathAttribute` for an explicit model-bound path. Method-level paths take precedence over a type-level path. Give different queries distinct paths; do not put multiple methods under one identical type-level path and expect separate endpoints.

For example, a query configured with `[Path("/api/accounts/by-id")]` takes its GUID-backed `AccountId` as `/api/accounts/by-id?id=11111111-1111-1111-1111-111111111111`, not as an appended route segment. Use [controller route templates](../controller-based/route-templates.md) when you need route-value binding.

## Validation attributes

The current model-bound `DataAnnotationValidationFilter` reads validation attributes from the **parameter type**, not from the method parameter's attributes, and does not recursively inspect DTO properties. `[Required]`, `[MinLength]`, or `[Range]` on a static query parameter therefore do not enforce the advertised rule. This is a current limitation, not a recommendation to omit validation.

Use the complete [FluentValidation argument-set example](../validation.md). MVC DataAnnotations follow MVC's separate rules.
