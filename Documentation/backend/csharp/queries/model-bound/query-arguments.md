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

It does **not** provide general nested-JSON DTO binding or array/list deserialization. A JSON object in `arguments` does not make an arbitrary `SearchCriteria` parameter bindable. Repeated GET keys bind only to [collections of those scalar types](#collection-arguments), not to collections of objects. Unsupported conversion can yield a missing/null/default value or a conversion error, rather than a useful DTO.

These limitations concern the supplied HTTP readers. Already-typed arguments passed directly to `IQueryPipeline`, custom readers/converters, and [MVC DTO binding](../controller-based/query-arguments.md) are different paths. FluentValidation's ability to traverse an object does not prove HTTP can construct that object.

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static IEnumerable<DebitAccount> GetAccountsByStatus(
        AccountStatus status,
        IMongoCollection<DebitAccount> collection)
    {
        // Implement status filtering logic
        return status switch
        {
            AccountStatus.Active => collection.Find(a => a.Balance > 0).ToList(),
            AccountStatus.Inactive => collection.Find(a => a.Balance == 0).ToList(),
            AccountStatus.Suspended => collection.Find(a => a.Balance < 0).ToList(),
            _ => collection.Find(_ => false).ToList()
        };
    }

    // A nullable enum works the same way — omit it from the query string to search across every status.
    public static IEnumerable<DebitAccount> GetAccountsByOptionalStatus(
        AccountStatus? status,
        IMongoCollection<DebitAccount> collection)
    {
        return status.HasValue
            ? collection.Find(a => a.Balance > 0).ToList()
            : collection.Find(_ => true).ToList();
    }
}
```

Arc classifies a method parameter as a caller-supplied query argument — rather than a value resolved from the dependency injection container — when it is a primitive, a concept, an enum (plain or nullable), or a collection of primitives, concepts, or enums. Anything else is injected when the container can supply it, as with `IMongoCollection<T>` or `ILogger<T>`. A type the container does not know, such as an unregistered plain class, is not injected: Arc treats it as a query argument, which the HTTP readers cannot construct. This is why `AccountStatus`/`AccountStatus?` above are read from the query string while `IMongoCollection<DebitAccount>` is resolved from the container in the same method signature.

### Collection Arguments

A collection parameter — `IEnumerable<T>`, an array, or `List<T>` — is classified the same way as a scalar one: it is a caller-supplied argument whenever its element type is a primitive, a concept, or an enum. Everything else about it works the same as a single value; the caller just sends the argument name repeated once per value (`?ids=1&ids=2&ids=3`), and Arc binds it back into the collection type your method declares.

```csharp
[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, CustomerId Owner, decimal Balance)
{
    public static IEnumerable<DebitAccount> GetAccountsByIds(
        IEnumerable<AccountId> ids,
        IMongoCollection<DebitAccount> collection)
    {
        return collection.Find(a => ids.Contains(a.Id)).ToList();
    }
    
    public static IEnumerable<DebitAccount> GetAccountsByOwners(
        List<CustomerId> ownerIds,
        IMongoCollection<DebitAccount> collection)
    {
        return collection.Find(a => ownerIds.Contains(a.Owner)).ToList();
    }

    public static IEnumerable<DebitAccount> GetAccountsByStatuses(
        IEnumerable<AccountStatus> statuses,
        IMongoCollection<DebitAccount> collection)
    {
        // Same derived-status logic as GetAccountsByStatus above, matched against any of the requested statuses.
        return collection.Find(_ => true).ToList().Where(a => statuses.Any(status => status switch
        {
            AccountStatus.Active => a.Balance > 0,
            AccountStatus.Inactive => a.Balance == 0,
            AccountStatus.Suspended => a.Balance < 0,
            _ => false
        }));
    }
}
```

> **The classification rule, stated once:** a parameter is caller-supplied when it is a primitive, a concept, an enum, **or a collection of those** — plain, nullable, or wrapped in `IEnumerable<T>`/an array/`List<T>` makes no difference. Everything else — a class, an interface, or a collection of any other element type such as `IEnumerable<IMongoCollection<T>>` — is resolved from the dependency injection container when it is registered there; an unregistered type falls back to being a query argument.

## Missing, empty, and optional values

Argument names match case-insensitively. GET and QUERY readers skip empty string values. At invocation, absent optional arguments use the method's default value. Nullable value types can be omitted; nonnullable value types without defaults are required. Concept nullability/defaults affect requiredness.

Plain reference types, including `string`, are implicitly optional in the current performer even without a nullable annotation. **A `string` parameter alone is not a required-input rule.** Use a whole-argument [query validator](../validation.md) to reject missing or empty input. Put required dependencies before optional arguments, as in the example above.

## Reserved keys

The GET reader removes these names from ordinary arguments, case-insensitively:

| Keys                                              | Purpose                          |
| ------------------------------------------------- | -------------------------------- |
| `page`, `pageSize`                                | Arc paging context               |
| `sortBy`, `sortDirection`                         | Arc sorting context              |
| `waitForFirstResult`, `waitForFirstResultTimeout` | Observable HTTP snapshot control |

GET also reads the reserved keys case-insensitively (`sortby`, `SORTBY`, `PAGE`, and `PAGESIZE` are equivalent spellings).
Do not declare method parameters named `page` or `pageSize` expecting GET to populate them. Use [automatic paging](paging.md), or distinct business argument names if you implement a separate result cap. QUERY places paging/sorting in separate envelope properties; hub subscriptions also have dedicated paging/sorting fields. Keep the distinction explicit across transports.

## URL binding

Without `[Path]`, Arc derives a kebab-case path from the namespace and generated-API options, including the configured route prefix, skipped namespace segments, and query-name inclusion. It does not infer `/{id}` from a parameter named `id` or a method named `ById`.

Use `Cratis.Arc.Queries.ModelBound.PathAttribute` for an explicit model-bound path. Method-level paths take precedence over a type-level path. Give different queries distinct paths; do not put multiple methods under one identical type-level path and expect separate endpoints.

For example, a query configured with `[Path("/api/accounts/by-id")]` takes its GUID-backed `AccountId` as `/api/accounts/by-id?id=11111111-1111-1111-1111-111111111111`, not as an appended route segment. Use [controller route templates](../controller-based/route-templates.md) when you need route-value binding.

## Validation attributes

The current model-bound `DataAnnotationValidationFilter` reads validation attributes from the **parameter type**, not from the method parameter's attributes, and does not recursively inspect DTO properties. `[Required]`, `[MinLength]`, or `[Range]` on a static query parameter therefore do not enforce the advertised rule. This is a current limitation, not a recommendation to omit validation.

Use the complete [FluentValidation argument-set example](../validation.md). MVC DataAnnotations follow MVC's separate rules.
