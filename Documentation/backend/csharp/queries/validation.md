---
title: Validate Cratis Arc query arguments
description: Reject invalid Cratis Arc query arguments with FluentValidation or DataAnnotations, validate cross-field rules, and handle HTTP validation failures.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Cratis Arc query validation rejects invalid input before the query method reads storage. Use a FluentValidation argument model when fields must be checked together, or DataAnnotations for rules on individual model-bound parameters. MVC actions use a separate model-binding and model-state path, so choose the example for the endpoint style you expose.

Validators of model-bound queries can read the operation's tenant and caller with `GetTenant()` and `GetCausedBy()`. See [reading the tenant and caller](../commands/validation.md#reading-the-tenant-and-caller).

## Validate the whole argument set

For an account search, reject a short prefix or a reversed balance range before sending the filter to MongoDB. An argument model lets the validator inspect those values together while the HTTP request still supplies flat arguments.

This alternative banking declaration uses the [shared `AccountId` and `AccountName` concepts](model-bound/index.md#model-account-identities-and-names), the Arc MongoDB provider, and validator discovery. The prefix is search text rather than a complete account name, so its length rule belongs to this query:

```csharp
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using FluentValidation;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Banking.Accounts;

[ReadModel]
public record DebitAccount(AccountId Id, AccountName Name, decimal Balance)
{
    [Path("/api/accounts/search")]
    public static IEnumerable<DebitAccount> Search(
        string prefix,
        decimal minimumBalance,
        decimal maximumBalance,
        IMongoCollection<DebitAccount> collection)
    {
        var namePrefix = new BsonRegularExpression("^" + Regex.Escape(prefix));
        var filter = Builders<DebitAccount>.Filter.Regex(account => account.Name, namePrefix) &
            Builders<DebitAccount>.Filter.Gte(account => account.Balance, minimumBalance) &
            Builders<DebitAccount>.Filter.Lte(account => account.Balance, maximumBalance);
        return collection.Find(filter).SortBy(account => account.Id).Limit(100).ToList();
    }
}

public class SearchParameters
{
    public string Prefix { get; set; } = string.Empty;
    public decimal MinimumBalance { get; set; }
    public decimal MaximumBalance { get; set; }
}

public class SearchValidator : QueryValidator<SearchParameters>
{
    public SearchValidator()
    {
        RuleFor(parameters => parameters.Prefix).NotEmpty().Length(3, 50);
        RuleFor(parameters => parameters.MinimumBalance).GreaterThanOrEqualTo(0);
        RuleFor(parameters => parameters.MaximumBalance)
            .GreaterThanOrEqualTo(parameters => parameters.MinimumBalance);
    }
}
```

```http
GET /api/accounts/search?prefix=Sav&minimumBalance=0&maximumBalance=1000
```

Arc constructs `SearchParameters` internally for validation; the method does not receive it as an HTTP DTO. The request above passes the rules. To check rejection, reverse the range:

```http
GET /api/accounts/search?prefix=Sav&minimumBalance=1000&maximumBalance=0
```

The validation verdict returns HTTP 400 before `Search` executes. Omitting `prefix` or supplying fewer than three characters also fails. Inspect `validationResults` for the failing member rather than treating this as an empty search result.

Once validation succeeds, `Regex.Escape` keeps the prefix literal in the MongoDB filter. The 100-row limit is a fixed cap, not automatic paging. Add [authorization](model-bound/authorization.md) and caller-scoped data selection before exposing private accounts; a valid search is not proof of access.

## Argument-model discovery

Arc looks in the read model's assembly for a type named `{QueryName}Parameters` or `{ReadModelName}{QueryName}Parameters`. Its properties must match every caller argument by name and type; injected dependencies such as the MongoDB collection do not count. Use unambiguous names within the assembly.

When Arc can construct that model, the FluentValidation filter validates it as one object graph. Otherwise it validates supplied arguments individually. That fallback skips missing or null arguments and cannot preserve cross-field rules declared only on the argument model. The performer enforces its own required-argument rules, but plain strings are implicitly optional there. This is why the example's whole-argument `NotEmpty` rule matters.

Test omitted values as well as explicit ones. Absent members retain their type defaults in the validation object; optional method defaults are not copied into it.

## Concepts and nested validation

A `ConceptValidator<T>` defines invariants for a strongly typed value and can apply to supplied query arguments as well as command properties. FluentValidation traverses the object graph, including nested values and collections, and reports failures using the argument/member path. Concept `Value` failures map to the containing field rather than exposing `.Value` to the client.

Validation can inspect an object only after the input path has constructed it. Built-in model-bound GET/QUERY readers support [scalar values and supported collections](model-bound/query-arguments.md#supported-input-shapes), not arbitrary nested DTO binding. Already-typed pipeline inputs and custom converters are separate paths to verify.

Generated clients extract supported FluentValidation rules for early feedback. Keep server validation authoritative: custom and asynchronous rules do not necessarily run in the browser. See [proxy validation](../proxy-generation/validation.md) for what generated clients support.

## DataAnnotations on parameters

For a rule on one argument, apply DataAnnotations such as `[Range(1, 100)]`, `[Required]`, `[StringLength(3)]`, or `[RegularExpression("^[A-Z]+$")]` directly to a public static model-bound query parameter. Arc generates metadata for the annotated parameter and evaluates the attributes against its bound value. Custom `ValidationAttribute` implementations are supported; concept arguments are validated against their underlying value.

Validation errors identify the parameter by name; `[Display]` controls its name in error messages. `[Required]` rejects null or omitted values. Other attributes follow DataAnnotations null semantics: an omitted nullable `[Range]` parameter is valid, while a missing non-nullable value type reports that the field is required.

This filter does not recursively validate DTO properties. Parameters without generated metadata retain the type-level attribute fallback; method-parameter annotations in assemblies built without Arc's generator are not evaluated by that fallback. Use FluentValidation for whole-argument and cross-field rules.

## Controller-based validation

For a controller action, follow [the MVC DTO example](controller-based/query-arguments.md#bind-and-validate-a-search-dto) and put annotations on DTO properties. MVC uses its own DataAnnotations and model-state behavior; positional-record metadata also differs from ordinary object-property validation.

Arc's GET action filter reads model state and can wrap validation errors. `[ApiController]`, custom filters, and `[AspNetResult]` affect which response runs first or whether wrapping happens. Verify the actual route's HTTP response before wiring a client to an assumed error envelope.

## Validation results and security

A model-bound validation failure has `isValid: false`, `isSuccess: false`, and entries in `validationResults` containing `message`, `members`, and `severity`. Member paths are camelCase, such as `maximumBalance` for the reversed range above. Nested validation can report paths such as `address.street` or `items[0].name`. Direct HTTP maps a validation verdict to 400; see [the full QueryResult contract](query-pipeline.md#query-result-metadata) for the remaining metadata, including paging.

Check forbidden callers independently of invalid input. Put access decisions in an [authorization verdict/filter](query-pipeline.md#query-filters), not in a validation rule. Then exercise missing, empty, malformed, out-of-range, and cross-field-invalid values through each GET, QUERY, or hub path your application exposes.
